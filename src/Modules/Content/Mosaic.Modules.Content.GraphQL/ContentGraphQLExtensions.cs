using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosaic.Modules.Content.GraphQL.ContentTypes;
using Mosaic.Modules.Content.GraphQL.Dynamic;

namespace Mosaic.Modules.Content.GraphQL;

public static class ContentGraphQLExtensions
{
    public static IRequestExecutorBuilder AddContentGraphQL(
        this IRequestExecutorBuilder builder,
        DynamicContentTypeModule? dynamicContentTypeModule = null)
    {
        dynamicContentTypeModule ??= new DynamicContentTypeModule(
            new DynamicContentSchemaProvider(DynamicContentSchemaSnapshot.Empty));

        builder
            .AddMutationType(descriptor => descriptor.Name(OperationTypeNames.Mutation))
            .AddType<ContentItemDetailsType>()
            .AddType<ContentItemsPageType>()
            .AddTypeExtension<ContentQueries>()
            .AddTypeExtension<ContentMutations>();

        return builder.AddTypeModule<DynamicContentTypeModule>();
    }
}
