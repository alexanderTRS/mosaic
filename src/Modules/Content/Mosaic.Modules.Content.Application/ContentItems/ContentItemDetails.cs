using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed record ContentItemDetails(
    Guid Id,
    Guid ContentTypeId,
    string ContentTypeApiName,
    ContentItemStatus Status,
    string Data,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
