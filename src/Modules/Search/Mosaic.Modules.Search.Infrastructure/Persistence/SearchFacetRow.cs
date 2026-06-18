namespace Mosaic.Modules.Search.Infrastructure.Persistence;

public sealed class SearchFacetRow
{
    public string Value { get; set; } = string.Empty;

    public int Count { get; set; }
}
