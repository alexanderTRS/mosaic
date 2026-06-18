namespace Mosaic.Modules.Content.Infrastructure.Persistence;

public sealed class ContentItemVersionRecord
{
    public Guid Id { get; set; }

    public Guid ContentItemId { get; set; }

    public int Version { get; set; }

    public required string Status { get; set; }

    public required string Data { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
