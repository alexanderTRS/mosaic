namespace Mosaic.Modules.Content.Infrastructure.Persistence;

public sealed class ContentTypeRecord
{
    public Guid Id { get; set; }

    public required string ApiName { get; set; }

    public required string DisplayName { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int SchemaVersion { get; set; }

    public List<ContentFieldRecord> Fields { get; set; } = [];
}
