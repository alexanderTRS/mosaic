namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class AuditEventRecord
{
    public Guid Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? ActorUserName { get; set; }

    public required string Action { get; set; }

    public required string Subject { get; set; }

    public string? Details { get; set; }
}
