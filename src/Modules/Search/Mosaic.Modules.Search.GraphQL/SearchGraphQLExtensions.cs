using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosaic.Modules.Search.GraphQL.ContentSearch;

namespace Mosaic.Modules.Search.GraphQL;

public static class SearchGraphQLExtensions
{
    public static IRequestExecutorBuilder AddSearchGraphQL(this IRequestExecutorBuilder builder)
        => builder
            .AddTypeExtension<SearchQueries>()
            .AddTypeExtension<SearchMutations>();
}
