namespace Mosaic.Modules.Search.Application.ContentSearch;

public sealed record SearchContentItemsQuery(
    string Query,
    string? ContentTypeApiName,
    int Skip,
    int Take);
