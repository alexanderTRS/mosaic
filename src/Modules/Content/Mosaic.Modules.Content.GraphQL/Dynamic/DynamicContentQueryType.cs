using HotChocolate.Resolvers;
using HotChocolate.Types;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.GraphQL.Dynamic;

public sealed class DynamicContentQueryType : ObjectTypeExtension
{
    private readonly DynamicContentSchemaProvider schemaProvider;

    public DynamicContentQueryType(DynamicContentSchemaProvider schemaProvider)
    {
        this.schemaProvider = schemaProvider;
    }

    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        foreach (var contentType in schemaProvider.Snapshot.ContentTypes)
        {
            var dynamicObjectType = new DynamicContentObjectType(contentType);

            descriptor
                .Field(contentType.SingleFieldName)
                .Description($"Dynamic single-item query for published content type '{contentType.ApiName}'.")
                .Argument("id", argument => argument.Type<NonNullType<UuidType>>())
                .Type(dynamicObjectType)
                .Resolve(context => ResolveSingle(context, contentType.ApiName));

            descriptor
                .Field(contentType.CollectionFieldName)
                .Description($"Dynamic collection query for published content type '{contentType.ApiName}'.")
                .Argument("status", argument => argument.Type<EnumType<ContentItemStatus>>())
                .Argument("search", argument => argument.Type<StringType>())
                .Argument("orderBy", argument => argument.Type<StringType>())
                .Argument("descending", argument => argument.Type<BooleanType>())
                .Argument("skip", argument => argument.Type<IntType>())
                .Argument("take", argument => argument.Type<IntType>())
                .Type(new NonNullType(new ListType(new NonNullType(dynamicObjectType))))
                .Resolve(context => ResolveCollection(context, contentType.ApiName));
        }
    }

    private static async Task<ContentItemDetails?> ResolveSingle(
        IResolverContext context,
        string contentTypeApiName)
    {
        var id = context.ArgumentValue<Guid>("id");
        var handler = context.Service<GetContentItemHandler>();
        var item = await handler.Handle(new ContentItemId(id), context.RequestAborted);

        return item.ContentTypeApiName == contentTypeApiName ? item : null;
    }

    private static async Task<IReadOnlyCollection<ContentItemDetails>> ResolveCollection(
        IResolverContext context,
        string contentTypeApiName)
    {
        var handler = context.Service<ListContentItemsHandler>();
        var status = context.ArgumentValue<ContentItemStatus?>("status");
        var search = context.ArgumentValue<string?>("search");
        var orderBy = context.ArgumentValue<string?>("orderBy");
        var descending = context.ArgumentValue<bool?>("descending") ?? true;
        var skip = context.ArgumentValue<int?>("skip") ?? 0;
        var take = context.ArgumentValue<int?>("take") ?? 20;

        var page = await handler.Handle(
            new ListContentItemsQuery(
                contentTypeApiName,
                status,
                search,
                orderBy,
                descending,
                skip,
                take),
            context.RequestAborted);

        return page.Items;
    }
}
