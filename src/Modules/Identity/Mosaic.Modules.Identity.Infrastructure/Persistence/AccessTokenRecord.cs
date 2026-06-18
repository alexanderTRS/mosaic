namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class AccessTokenRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public string? Name { get; set; }

    public required string Kind { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
