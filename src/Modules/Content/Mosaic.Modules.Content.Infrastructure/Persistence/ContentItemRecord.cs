namespace Mosaic.Modules.Content.Infrastructure.Persistence;

public sealed class ContentItemRecord
{
    public Guid Id { get; set; }

    public Guid ContentTypeId { get; set; }

    public required string Status { get; set; }

    public required string Data { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
