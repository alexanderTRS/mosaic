namespace Mosaic.Modules.Search.Application.ContentSearch;

public sealed record SearchContentItemsPage(
    IReadOnlyCollection<SearchContentItemResult> Items,
    IReadOnlyCollection<SearchFacetValue> ContentTypeFacets,
    IReadOnlyCollection<SearchFacetValue> StatusFacets,
    int TotalCount,
    int Skip,
    int Take);
