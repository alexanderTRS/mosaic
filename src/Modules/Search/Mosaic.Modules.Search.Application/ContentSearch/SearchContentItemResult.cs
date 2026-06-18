namespace Mosaic.Modules.Search.Application.ContentSearch;

public sealed record SearchContentItemResult(
    Guid Id,
    string ContentTypeApiName,
    string Status,
    string Data,
    DateTimeOffset UpdatedAt,
    double Score);
