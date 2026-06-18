namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class UserGroupRecord
{
    public Guid UserId { get; set; }

    public Guid GroupId { get; set; }
}
