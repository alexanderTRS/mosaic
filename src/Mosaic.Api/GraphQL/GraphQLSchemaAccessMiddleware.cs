using System.Text;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Api.GraphQL;

public sealed class GraphQLSchemaAccessMiddleware
{
    private readonly RequestDelegate next;

    public GraphQLSchemaAccessMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context, ICurrentUserAccessor currentUserAccessor)
    {
        if (IsGraphQLIdeRequest(context) || await IsIntrospectionRequest(context))
        {
            var user = currentUserAccessor.CurrentUser;
            if (!user.IsAuthenticated || !user.CanViewGraphQLSchema)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        errors = new[]
                        {
                            new
                            {
                                message = "CanViewGraphQLSchema permission is required.",
                                extensions = new { code = "ACCESS_DENIED" }
                            }
                        }
                    },
                    context.RequestAborted);
                return;
            }
        }

        await next(context);
    }

    private static bool IsGraphQLIdeRequest(HttpContext context)
        => context.Request.Path.StartsWithSegments("/graphql/ui", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> IsIntrospectionRequest(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/graphql", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (HttpMethods.IsGet(context.Request.Method))
        {
            var query = context.Request.Query["query"].ToString();
            return ContainsIntrospectionField(query);
        }

        if (!HttpMethods.IsPost(context.Request.Method) || context.Request.Body is null)
        {
            return false;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;

        return ContainsIntrospectionField(body);
    }

    private static bool ContainsIntrospectionField(string value)
        => value.Contains("__schema", StringComparison.Ordinal)
            || value.Contains("__type", StringComparison.Ordinal);
}
