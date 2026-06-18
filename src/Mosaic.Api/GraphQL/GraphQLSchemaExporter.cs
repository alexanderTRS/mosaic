using HotChocolate.Execution;

namespace Mosaic.Api.GraphQL;

public static class GraphQLSchemaExporter
{
    public static async Task Export(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var executorResolver = app.Services.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync();
        await Export(app.Environment, executor.Schema.Print(), CancellationToken.None);
    }

    public static async Task Export(
        IWebHostEnvironment environment,
        string schema,
        CancellationToken cancellationToken)
    {
        var path = System.IO.Path.Combine(environment.ContentRootPath, "..", "..", "docs", "graphql");
        Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(
            System.IO.Path.Combine(path, "schema.graphql"),
            schema,
            cancellationToken);
    }
}
