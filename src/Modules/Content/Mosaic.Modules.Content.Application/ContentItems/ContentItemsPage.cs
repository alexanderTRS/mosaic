namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed record ContentItemsPage(
    IReadOnlyCollection<ContentItemDetails> Items,
    int TotalCount,
    int Skip,
    int Take);
