namespace Mosaic.Modules.Identity.Application.Management;

public interface IIdentityManagementRepository
{
    Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken);

    Task<UserDetails?> GetUser(Guid userId, CancellationToken cancellationToken);

    Task<RoleDetails?> GetRole(Guid roleId, CancellationToken cancellationToken);

    Task<GroupDetails?> GetGroup(Guid groupId, CancellationToken cancellationToken);

    Task AddUser(UserDetails user, string passwordHash, CancellationToken cancellationToken);

    Task AddRole(RoleDetails role, CancellationToken cancellationToken);

    Task AddGroup(GroupDetails group, CancellationToken cancellationToken);

    Task GrantContentTypeAccess(
        Guid userId,
        ContentTypePermissionGrant permission,
        CancellationToken cancellationToken);

    Task GrantRoleContentTypeAccess(
        Guid roleId,
        ContentTypePermissionGrant permission,
        CancellationToken cancellationToken);

    Task AssignRoleToUser(Guid userId, Guid roleId, CancellationToken cancellationToken);

    Task AssignUserToGroup(Guid userId, Guid groupId, CancellationToken cancellationToken);

    Task AssignRoleToGroup(Guid groupId, Guid roleId, CancellationToken cancellationToken);

    Task AddServiceAccountToken(
        Guid serviceAccountId,
        string tokenHash,
        string name,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task SaveChanges(CancellationToken cancellationToken);
}
