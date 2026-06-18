namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class UserRoleRecord
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }
}
