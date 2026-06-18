using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosaic.Modules.Media.GraphQL.Media;

namespace Mosaic.Modules.Media.GraphQL;

public static class MediaGraphQLExtensions
{
    public static IRequestExecutorBuilder AddMediaGraphQL(this IRequestExecutorBuilder builder)
        => builder
            .AddTypeExtension<MediaQueries>()
            .AddTypeExtension<MediaMutations>();
}
