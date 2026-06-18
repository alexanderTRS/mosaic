namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class ExternalIdentityRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string Provider { get; set; }

    public required string Subject { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public DateTimeOffset LinkedAt { get; set; }
}
