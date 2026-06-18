using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed record ListContentItemsQuery(
    string? ContentTypeApiName,
    ContentItemStatus? Status,
    string? Search,
    string? OrderBy,
    bool Descending,
    int Skip,
    int Take);
