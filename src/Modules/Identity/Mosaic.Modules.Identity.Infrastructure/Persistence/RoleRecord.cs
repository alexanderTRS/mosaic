namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class RoleRecord
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string DisplayName { get; set; }

    public required string Preset { get; set; }

    public bool CanCreateContentTypes { get; set; }

    public bool CanViewGraphQLSchema { get; set; }
}
