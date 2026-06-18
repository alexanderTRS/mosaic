namespace Mosaic.Modules.Search.Infrastructure.Persistence;

public sealed class SearchContentItemRow
{
    public Guid Id { get; set; }

    public string ContentTypeApiName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Data { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public double Score { get; set; }

    public int TotalCount { get; set; }
}
