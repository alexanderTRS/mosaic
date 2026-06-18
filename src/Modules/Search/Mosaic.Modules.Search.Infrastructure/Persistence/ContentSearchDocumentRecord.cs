namespace Mosaic.Modules.Search.Infrastructure.Persistence;

public sealed class ContentSearchDocumentRecord
{
    public Guid Id { get; set; }

    public Guid ContentItemId { get; set; }

    public string ContentTypeApiName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Data { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
