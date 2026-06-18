namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class GroupRecord
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string DisplayName { get; set; }
}
