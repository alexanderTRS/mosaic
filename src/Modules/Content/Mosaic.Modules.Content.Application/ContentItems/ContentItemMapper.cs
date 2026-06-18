using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.Application.ContentItems;

internal static class ContentItemMapper
{
    public static ContentItemDetails ToDetails(ContentItem item, string contentTypeApiName)
        => new(
            item.Id.Value,
            item.ContentTypeId.Value,
            contentTypeApiName,
            item.Status,
            item.Data,
            item.CreatedAt,
            item.UpdatedAt);
}
