using HotChocolate;
using HotChocolate.Types;
using Mosaic.Modules.Content.Application.ContentItems;
using Microsoft.Extensions.Logging;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.GraphQL.ContentTypes;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ContentQueries
{
    public string ContentModuleStatus([Service] ILogger<ContentQueries> logger)
    {
        logger.LogDebug("Content module status queried through GraphQL.");
        return "ready";
    }

    public Task<IReadOnlyCollection<ContentTypeDetails>> ContentTypes(
        [Service] ListContentTypesHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(cancellationToken);

    public Task<IReadOnlyCollection<ContentItemDetails>> ContentItems(
        string? contentTypeApiName,
        [Service] ListContentItemsHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(contentTypeApiName, cancellationToken);

    public Task<ContentItemsPage> ContentItemsPage(
        string? contentTypeApiName,
        ContentItemStatus? status,
        string? search,
        string? orderBy,
        bool descending,
        int skip,
        int take,
        [Service] ListContentItemsHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new ListContentItemsQuery(
                contentTypeApiName,
                status,
                search,
                orderBy,
                descending,
                skip,
                take),
            cancellationToken);

    public Task<ContentItemDetails> ContentItem(
        Guid id,
        [Service] GetContentItemHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(new ContentItemId(id), cancellationToken);

    public Task<IReadOnlyCollection<ContentItemVersionDetails>> ContentItemVersions(
        Guid id,
        [Service] ListContentItemVersionsHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(new ContentItemId(id), cancellationToken);
}
