using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mosaic.Modules.Abstractions;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Identity.Application.AccessTokens;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Application.Management;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.Modules.Identity.Infrastructure.Security;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;
using Mosaic.SharedKernel.Time;
using System.Security.Claims;

namespace Mosaic.Modules.Identity.Infrastructure;

public sealed class IdentityModule : IMosaicModule
{
    public string Name => "Identity";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Mosaic")
            ?? "Host=localhost;Port=5432;Database=mosaic;Username=mosaic;Password=mosaic";

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<IdentitySecurityOptions>(configuration.GetSection("Identity:Security"));
        services.Configure<OidcIdentityOptions>(configuration.GetSection("Identity:Oidc"));

        services.TryAddSingleton<IClock, Mosaic.SharedKernel.Time.SystemClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IPasswordPolicy, ConfiguredPasswordPolicy>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
        services.AddScoped<IIdentityManagementRepository, IdentityManagementRepository>();
        services.AddScoped<IAccessTokenService, AccessTokenService>();
        services.AddScoped<IContentAccessService, ContentAccessService>();
        services.AddScoped<IAuditLog, EfAuditLog>();
        services.AddScoped<ExternalLoginProvisioner>();
        services.AddScoped(provider =>
        {
            var options = configuration
                .GetSection("Identity:Security")
                .Get<IdentitySecurityOptions>() ?? new IdentitySecurityOptions();

            return new LoginHandler(
                provider.GetRequiredService<IUserCredentialRepository>(),
                provider.GetRequiredService<IPasswordHasher>(),
                provider.GetRequiredService<ITokenGenerator>(),
                provider.GetRequiredService<IAuditLog>(),
                provider.GetRequiredService<IClock>(),
                TimeSpan.FromMinutes(Math.Max(1, options.AccessTokenLifetimeMinutes)),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LoginHandler>>());
        });
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<GrantContentTypeAccessHandler>();
        services.AddScoped<CreateRoleHandler>();
        services.AddScoped<CreateServiceAccountHandler>();
        services.AddScoped<CreateServiceAccountTokenHandler>();
        services.AddScoped<CreateGroupHandler>();
        services.AddScoped<AssignRoleToUserHandler>();
        services.AddScoped<AssignUserToGroupHandler>();
        services.AddScoped<AssignRoleToGroupHandler>();
        services.AddScoped<GrantRoleContentTypeAccessHandler>();
        services.AddScoped<RevokeAccessTokenHandler>();
        services.AddHostedService<IdentitySeeder>();

        var oidcOptions = configuration.GetSection("Identity:Oidc").Get<OidcIdentityOptions>() ?? new OidcIdentityOptions();
        var authenticationBuilder = services
            .AddAuthentication("Mosaic")
            .AddPolicyScheme(
                "Mosaic",
                "Mosaic bearer or cookie authentication",
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var authorization = context.Request.Headers.Authorization.ToString();
                        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            ? BearerTokenAuthenticationHandler.SchemeName
                            : CookieAuthenticationDefaults.AuthenticationScheme;
                    };
                })
            .AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.Cookie.Name = "Mosaic.Auth";
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.SlidingExpiration = true;
                })
            .AddScheme<AuthenticationSchemeOptions, BearerTokenAuthenticationHandler>(
                BearerTokenAuthenticationHandler.SchemeName,
                options => { });

        if (oidcOptions.Enabled)
        {
            if (string.IsNullOrWhiteSpace(oidcOptions.Authority)
                || string.IsNullOrWhiteSpace(oidcOptions.ClientId))
            {
                throw new InvalidOperationException(
                    "Identity:Oidc:Authority and Identity:Oidc:ClientId are required when OIDC is enabled.");
            }

            authenticationBuilder.AddOpenIdConnect(
                oidcOptions.Scheme,
                options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = oidcOptions.Authority;
                    options.ClientId = oidcOptions.ClientId;
                    options.ClientSecret = oidcOptions.ClientSecret;
                    options.CallbackPath = oidcOptions.CallbackPath;
                    options.RequireHttpsMetadata = oidcOptions.RequireHttpsMetadata;
                    options.ResponseType = "code";
                    options.SaveTokens = false;
                    options.Scope.Add("email");
                    options.Scope.Add("profile");
                    options.Events.OnTokenValidated = async context =>
                    {
                        var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? context.Principal?.FindFirstValue("sub");
                        if (string.IsNullOrWhiteSpace(subject))
                        {
                            context.Fail("OIDC subject claim is missing.");
                            return;
                        }

                        var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? context.Principal?.FindFirstValue("email");
                        var displayName = context.Principal?.FindFirstValue(ClaimTypes.Name)
                            ?? context.Principal?.FindFirstValue("name")
                            ?? email;
                        var provisioner = context.HttpContext.RequestServices
                            .GetRequiredService<ExternalLoginProvisioner>();
                        var user = await provisioner.FindOrProvision(
                            oidcOptions.Provider,
                            subject,
                            email,
                            displayName,
                            oidcOptions.AutoProvisionUsers,
                            oidcOptions.DefaultRoleNames,
                            context.HttpContext.RequestAborted);

                        if (user is null)
                        {
                            context.Fail("External identity is not linked to a Mosaic user.");
                            return;
                        }

                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim("is_administrator", user.IsAdministrator.ToString()),
                            new Claim("can_view_graphql_schema", user.CanViewGraphQLSchema.ToString())
                        };
                        context.Principal = new ClaimsPrincipal(
                            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
                    };
                });
        }
    }
}
