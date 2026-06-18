using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Identity.Application.Management;

namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityManagementRepository : IIdentityManagementRepository
{
    private readonly IdentityDbContext dbContext;

    public IdentityManagementRepository(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken)
        => dbContext.Users.AnyAsync(user => user.UserName == userName.Trim(), cancellationToken);

    public async Task<UserDetails?> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

        return user is null
            ? null
            : new UserDetails(
                user.Id,
                user.UserName,
                user.IsAdministrator,
                user.IsAdministrator || user.CanViewGraphQLSchema,
                user.IsServiceAccount);
    }

    public async Task<RoleDetails?> GetRole(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.AsNoTracking()
            .SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

        return role is null
            ? null
            : ToRoleDetails(role);
    }

    public async Task<GroupDetails?> GetGroup(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await dbContext.Groups.AsNoTracking()
            .SingleOrDefaultAsync(group => group.Id == groupId, cancellationToken);

        return group is null
            ? null
            : new GroupDetails(group.Id, group.Name, group.DisplayName);
    }

    public async Task AddUser(UserDetails user, string passwordHash, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(
            new UserRecord
            {
                Id = user.Id,
                UserName = user.UserName,
                PasswordHash = passwordHash,
                IsAdministrator = user.IsAdministrator,
                CanViewGraphQLSchema = user.IsAdministrator || user.CanViewGraphQLSchema,
                IsServiceAccount = user.IsServiceAccount
            },
            cancellationToken);
    }

    public async Task AddRole(RoleDetails role, CancellationToken cancellationToken)
    {
        await dbContext.Roles.AddAsync(
            new RoleRecord
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Preset = role.Preset.ToString(),
                CanCreateContentTypes = role.CanCreateContentTypes,
                CanViewGraphQLSchema = role.CanViewGraphQLSchema
            },
            cancellationToken);

        if (role.Preset == PermissionPreset.Administrator)
        {
            await GrantRoleContentTypeAccess(
                role.Id,
                new ContentTypePermissionGrant(
                    "*",
                    CanManageSchema: true,
                    CanManageItems: true,
                    CanReadItems: true),
                cancellationToken);
        }
    }

    public async Task AddGroup(GroupDetails group, CancellationToken cancellationToken)
    {
        await dbContext.Groups.AddAsync(
            new GroupRecord
            {
                Id = group.Id,
                Name = group.Name,
                DisplayName = group.DisplayName
            },
            cancellationToken);
    }

    public async Task GrantContentTypeAccess(
        Guid userId,
        ContentTypePermissionGrant permission,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ContentTypePermissions.SingleOrDefaultAsync(
            record => record.UserId == userId
                && record.ContentTypeApiName == permission.ContentTypeApiName
                && record.FieldApiName == permission.FieldApiName
                && record.Locale == permission.Locale,
            cancellationToken);

        if (existing is null)
        {
            await dbContext.ContentTypePermissions.AddAsync(
                new ContentTypePermissionRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ContentTypeApiName = permission.ContentTypeApiName,
                    FieldApiName = permission.FieldApiName,
                    Locale = permission.Locale,
                    CanManageSchema = permission.CanManageSchema,
                    CanManageItems = permission.CanManageItems,
                    CanReadItems = permission.CanReadItems
                },
                cancellationToken);
            return;
        }

        existing.CanManageSchema = permission.CanManageSchema;
        existing.CanManageItems = permission.CanManageItems;
        existing.CanReadItems = permission.CanReadItems;
    }

    public async Task GrantRoleContentTypeAccess(
        Guid roleId,
        ContentTypePermissionGrant permission,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.RoleContentTypePermissions.SingleOrDefaultAsync(
            record => record.RoleId == roleId
                && record.ContentTypeApiName == permission.ContentTypeApiName
                && record.FieldApiName == permission.FieldApiName
                && record.Locale == permission.Locale,
            cancellationToken);

        if (existing is null)
        {
            await dbContext.RoleContentTypePermissions.AddAsync(
                new RoleContentTypePermissionRecord
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    ContentTypeApiName = permission.ContentTypeApiName,
                    FieldApiName = permission.FieldApiName,
                    Locale = permission.Locale,
                    CanManageSchema = permission.CanManageSchema,
                    CanManageItems = permission.CanManageItems,
                    CanReadItems = permission.CanReadItems
                },
                cancellationToken);
            return;
        }

        existing.CanManageSchema = permission.CanManageSchema;
        existing.CanManageItems = permission.CanManageItems;
        existing.CanReadItems = permission.CanReadItems;
    }

    public async Task AssignRoleToUser(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserRoles.AnyAsync(
            record => record.UserId == userId && record.RoleId == roleId,
            cancellationToken))
        {
            return;
        }

        await dbContext.UserRoles.AddAsync(
            new UserRoleRecord { UserId = userId, RoleId = roleId },
            cancellationToken);
    }

    public async Task AssignUserToGroup(Guid userId, Guid groupId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserGroups.AnyAsync(
            record => record.UserId == userId && record.GroupId == groupId,
            cancellationToken))
        {
            return;
        }

        await dbContext.UserGroups.AddAsync(
            new UserGroupRecord { UserId = userId, GroupId = groupId },
            cancellationToken);
    }

    public async Task AssignRoleToGroup(Guid groupId, Guid roleId, CancellationToken cancellationToken)
    {
        if (await dbContext.GroupRoles.AnyAsync(
            record => record.GroupId == groupId && record.RoleId == roleId,
            cancellationToken))
        {
            return;
        }

        await dbContext.GroupRoles.AddAsync(
            new GroupRoleRecord { GroupId = groupId, RoleId = roleId },
            cancellationToken);
    }

    public async Task AddServiceAccountToken(
        Guid serviceAccountId,
        string tokenHash,
        string name,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await dbContext.AccessTokens.AddAsync(
            new AccessTokenRecord
            {
                Id = Guid.NewGuid(),
                UserId = serviceAccountId,
                TokenHash = tokenHash,
                Name = name,
                Kind = "ServiceAccount",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = expiresAt
            },
            cancellationToken);
    }

    public Task SaveChanges(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    private static RoleDetails ToRoleDetails(RoleRecord role)
        => new(
            role.Id,
            role.Name,
            role.DisplayName,
            Enum.Parse<PermissionPreset>(role.Preset),
            role.CanCreateContentTypes,
            role.CanViewGraphQLSchema);
}
