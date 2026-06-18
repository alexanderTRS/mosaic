using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Infrastructure.Security;

namespace Mosaic.Api.Auth;

public static class LoginEndpoint
{
    public static async Task<IResult> SignIn(
        HttpContext httpContext,
        LoginRequest request,
        LoginHandler loginHandler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await loginHandler.Handle(
                new LoginCommand(request.UserName, request.Password),
                cancellationToken);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                new Claim(ClaimTypes.Name, result.UserName),
                new Claim("is_administrator", result.IsAdministrator.ToString()),
                new Claim("can_view_graphql_schema", result.CanViewGraphQLSchema.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return Results.Redirect(SafeReturnUrl(request.ReturnUrl));
        }
        catch (LoginFailedException)
        {
            return LoginPage.Render(httpContext, request.ReturnUrl, "Invalid username or password.");
        }
    }

    public static async Task<IResult> SignOut(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    public static IResult ChallengeOidc(
        HttpContext httpContext,
        IConfiguration configuration,
        string? returnUrl)
    {
        var options = configuration.GetSection("Identity:Oidc").Get<OidcIdentityOptions>() ?? new OidcIdentityOptions();
        if (!options.Enabled)
        {
            return LoginPage.Render(httpContext, returnUrl, "External login is not configured.");
        }

        return Results.Challenge(
            new AuthenticationProperties
            {
                RedirectUri = SafeReturnUrl(returnUrl)
            },
            [options.Scheme]);
    }

    private static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return "/graphql/ui";
        }

        return returnUrl;
    }
}
