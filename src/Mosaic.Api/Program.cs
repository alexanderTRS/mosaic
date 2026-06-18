using System.Security.Claims;
using Mosaic.Api.Auth;
using Mosaic.Api.GraphQL;
using Mosaic.Modules.Abstractions;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.GraphQL;
using Mosaic.Modules.Content.GraphQL.Dynamic;
using Mosaic.Modules.Content.Infrastructure;
using Mosaic.Modules.Identity.GraphQL;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Infrastructure;
using Mosaic.Modules.Media.Application.MediaAssets;
using Mosaic.Modules.Media.GraphQL;
using Mosaic.Modules.Media.Infrastructure;
using Mosaic.Modules.Search.GraphQL;
using Mosaic.Modules.Search.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter(renderMessage: true))
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Mosaic API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Services.AddSerilog((services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Mosaic.Api");

        if (builder.Configuration.GetValue("Observability:ConsoleJson", true))
        {
            configuration.WriteTo.Console(new JsonFormatter(renderMessage: true));
        }
        else
        {
            configuration.WriteTo.Console();
        }
    });

    builder.Services.AddHealthChecks();

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    builder.Services.AddMosaicModule<IdentityModule>(builder.Configuration);
    builder.Services.AddMosaicModule<ContentModule>(builder.Configuration);
    builder.Services.AddMosaicModule<MediaModule>(builder.Configuration);
    builder.Services.AddMosaicModule<SearchModule>(builder.Configuration);

    var dynamicContentSchemaSnapshot = await DynamicContentSchemaSnapshotLoader.Load(
        builder.Configuration,
        CancellationToken.None);
    var dynamicContentSchemaProvider = new DynamicContentSchemaProvider(dynamicContentSchemaSnapshot);
    var dynamicContentTypeModule = new DynamicContentTypeModule(dynamicContentSchemaProvider);
    builder.Services.AddSingleton(dynamicContentSchemaProvider);
    builder.Services.AddSingleton(dynamicContentTypeModule);
    builder.Services.RemoveAll<IContentSchemaChangeNotifier>();
    builder.Services.AddScoped<IContentSchemaChangeNotifier, HotChocolateContentSchemaChangeNotifier>();

    builder.Services
        .AddGraphQLServer()
        .ModifyRequestOptions(options => options.IncludeExceptionDetails = builder.Environment.IsDevelopment())
        .AddQueryType<Query>()
        .AddErrorFilter<MosaicErrorFilter>()
        .AddIdentityGraphQL()
        .AddContentGraphQL(dynamicContentTypeModule)
        .AddMediaGraphQL()
        .AddSearchGraphQL();

    var app = builder.Build();

    app.Use(
        async (context, next) =>
        {
            var correlationId = GetOrCreateCorrelationId(context);
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
            {
                await next(context);
            }
        });

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", GetOrCreateCorrelationId(httpContext));
            diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);

            var user = httpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserName", user.Identity.Name);
                diagnosticContext.Set("UserId", user.FindFirstValue(ClaimTypes.NameIdentifier));
                diagnosticContext.Set("IsAdministrator", user.FindFirstValue("is_administrator") == bool.TrueString);
            }
        };
    });
    app.UseAuthentication();
    app.UseCors();
    app.Use(
        async (context, next) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                await next(context);
                return;
            }

            using (LogContext.PushProperty("UserName", user.Identity.Name))
            using (LogContext.PushProperty("UserId", user.FindFirstValue(ClaimTypes.NameIdentifier)))
            using (LogContext.PushProperty("IsAdministrator", user.FindFirstValue("is_administrator") == bool.TrueString))
            {
                await next(context);
            }
        });
    app.UseMiddleware<GraphQLSchemaAccessMiddleware>();

    app.MapGet("/", () => Results.Ok(new { name = "Mosaic", status = "running" }));
    app.MapGet("/login", (HttpContext httpContext, string? returnUrl) => LoginPage.Render(httpContext, returnUrl));
    app.MapGet("/login/oidc", (Delegate)LoginEndpoint.ChallengeOidc);
    app.MapPost(
        "/login",
        async (
            HttpContext httpContext,
            LoginHandler loginHandler,
            CancellationToken cancellationToken) =>
        {
            var form = await httpContext.Request.ReadFormAsync(cancellationToken);
            var request = new LoginRequest(
                form["userName"].ToString(),
                form["password"].ToString(),
                form["returnUrl"].ToString());

            if (!CsrfTokenService.IsValid(httpContext, form[CsrfTokenService.FormFieldName].ToString()))
            {
                return LoginPage.Render(httpContext, request.ReturnUrl, "The login form expired. Please try again.");
            }

            return await LoginEndpoint.SignIn(httpContext, request, loginHandler, cancellationToken);
        });
    app.MapGet("/logout", (Delegate)LoginEndpoint.SignOut);
    app.MapGet(
        "/media/assets/{id:guid}/file",
        async (
            Guid id,
            OpenMediaAssetFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var file = await handler.Handle(id, cancellationToken);
            return Results.File(file.Content, file.ContentType, file.FileName);
        });
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    app.MapGraphQL("/graphql");
    app.MapNitroApp("/graphql/ui", "/graphql");

    await GraphQLSchemaExporter.Export(app);

    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Mosaic API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static string GetOrCreateCorrelationId(HttpContext context)
{
    const string headerName = "X-Correlation-Id";
    if (context.Request.Headers.TryGetValue(headerName, out var values))
    {
        var value = values.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return context.TraceIdentifier;
}

public partial class Program
{
}
