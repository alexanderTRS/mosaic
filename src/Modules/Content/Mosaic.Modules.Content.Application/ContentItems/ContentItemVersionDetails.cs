using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed record ContentItemVersionDetails(
    Guid Id,
    Guid ContentItemId,
    int Version,
    ContentItemStatus Status,
    string Data,
    DateTimeOffset CreatedAt);
