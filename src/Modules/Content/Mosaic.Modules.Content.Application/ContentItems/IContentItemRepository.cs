using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.Application.ContentItems;

public interface IContentItemRepository
{
    Task<ContentItem?> GetDomainById(ContentItemId contentItemId, CancellationToken cancellationToken);

    Task<ContentItemDetails?> GetById(ContentItemId contentItemId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ContentItemDetails>> List(
        string? contentTypeApiName,
        CancellationToken cancellationToken);

    Task<ContentItemsPage> Page(ListContentItemsQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ContentItemVersionDetails>> ListVersions(
        ContentItemId contentItemId,
        CancellationToken cancellationToken);

    Task Add(ContentItem contentItem, CancellationToken cancellationToken);

    Task Update(ContentItem contentItem, CancellationToken cancellationToken);

    Task AddVersion(ContentItem contentItem, CancellationToken cancellationToken);

    Task<bool> ExistsWithFieldValue(
        ContentTypeId contentTypeId,
        string fieldApiName,
        string fieldValueJson,
        ContentItemId? excludeContentItemId,
        CancellationToken cancellationToken);

    Task<bool> ExistsById(
        ContentTypeId contentTypeId,
        ContentItemId contentItemId,
        CancellationToken cancellationToken);
}
