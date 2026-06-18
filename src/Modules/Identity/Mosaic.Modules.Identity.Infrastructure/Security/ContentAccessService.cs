using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class ContentAccessService : IContentAccessService
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IdentityDbContext dbContext;

    public ContentAccessService(ICurrentUserAccessor currentUserAccessor, IdentityDbContext dbContext)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.dbContext = dbContext;
    }

    public async Task EnsureCanCreateContentType(CancellationToken cancellationToken)
    {
        var user = RequireAuthenticated();
        if (user.IsAdministrator)
        {
            return;
        }

        if (!await HasRolePermission(user.Id!.Value, role => role.CanCreateContentTypes, cancellationToken))
        {
            throw new AccessDeniedException("Only administrators or users with a role that can create content types can create content types.");
        }
    }

    public async Task EnsureCanManageContentType(string contentTypeApiName, CancellationToken cancellationToken)
    {
        var user = RequireAuthenticated();
        if (user.IsAdministrator)
        {
            return;
        }

        if (!await HasPermission(
            user.Id!.Value,
            contentTypeApiName,
            PermissionOperation.ManageSchema,
            cancellationToken))
        {
            throw new AccessDeniedException($"User cannot manage content type '{contentTypeApiName}'.");
        }
    }

    public async Task EnsureCanManageContentItems(string contentTypeApiName, CancellationToken cancellationToken)
    {
        var user = RequireAuthenticated();
        if (user.IsAdministrator)
        {
            return;
        }

        if (!await HasPermission(
            user.Id!.Value,
            contentTypeApiName,
            PermissionOperation.ManageItems,
            cancellationToken))
        {
            throw new AccessDeniedException($"User cannot manage items for content type '{contentTypeApiName}'.");
        }
    }

    public async Task EnsureCanManageContentFields(
        string contentTypeApiName,
        IReadOnlyCollection<ContentFieldAccessRequest> fields,
        CancellationToken cancellationToken)
    {
        var user = RequireAuthenticated();
        if (user.IsAdministrator)
        {
            return;
        }

        if (await HasPermission(
            user.Id!.Value,
            contentTypeApiName,
            PermissionOperation.ManageItems,
            cancellationToken))
        {
            return;
        }

        foreach (var field in fields)
        {
            if (!await HasFieldPermission(user.Id.Value, contentTypeApiName, field, cancellationToken))
            {
                throw new AccessDeniedException(
                    $"User cannot manage field '{field.FieldApiName}'"
                    + (field.Locale is null ? string.Empty : $" locale '{field.Locale}'")
                    + $" for content type '{contentTypeApiName}'.");
            }
        }
    }

    public async Task EnsureCanReadContentItems(string? contentTypeApiName, CancellationToken cancellationToken)
    {
        var user = RequireAuthenticated();
        if (user.IsAdministrator)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(contentTypeApiName))
        {
            throw new AccessDeniedException("Only administrators can list all content items.");
        }

        if (!await HasPermission(
            user.Id!.Value,
            contentTypeApiName,
            PermissionOperation.ReadItems,
            cancellationToken))
        {
            throw new AccessDeniedException($"User cannot read items for content type '{contentTypeApiName}'.");
        }
    }

    private CurrentUser RequireAuthenticated()
    {
        var user = currentUserAccessor.CurrentUser;
        if (!user.IsAuthenticated || user.Id is null)
        {
            throw new AccessDeniedException("Authentication is required.");
        }

        return user;
    }

    private async Task<bool> HasPermission(
        Guid userId,
        string contentTypeApiName,
        PermissionOperation operation,
        CancellationToken cancellationToken)
    {
        var permission = await dbContext.ContentTypePermissions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                permission => permission.UserId == userId
                    && permission.ContentTypeApiName == contentTypeApiName
                    && permission.FieldApiName == null
                    && permission.Locale == null,
                cancellationToken);

        if (permission is not null && HasUserPermission(permission, operation))
        {
            return true;
        }

        var roleIds = RoleIdsFor(userId);

        var rolePermissions = await dbContext.RoleContentTypePermissions
            .AsNoTracking()
            .Where(permission => roleIds.Contains(permission.RoleId)
                && (permission.ContentTypeApiName == contentTypeApiName || permission.ContentTypeApiName == "*")
                && permission.FieldApiName == null
                && permission.Locale == null)
            .ToListAsync(cancellationToken);

        return rolePermissions.Any(permission => HasRolePermission(permission, operation));
    }

    private async Task<bool> HasFieldPermission(
        Guid userId,
        string contentTypeApiName,
        ContentFieldAccessRequest field,
        CancellationToken cancellationToken)
    {
        var userPermissions = await dbContext.ContentTypePermissions
            .AsNoTracking()
            .Where(permission => permission.UserId == userId
                && permission.ContentTypeApiName == contentTypeApiName
                && (permission.FieldApiName == field.FieldApiName || permission.FieldApiName == null)
                && (permission.Locale == field.Locale || permission.Locale == null))
            .ToListAsync(cancellationToken);
        if (userPermissions.Any(permission => permission.CanManageItems))
        {
            return true;
        }

        var roleIds = RoleIdsFor(userId);
        var rolePermissions = await dbContext.RoleContentTypePermissions
            .AsNoTracking()
            .Where(permission => roleIds.Contains(permission.RoleId)
                && (permission.ContentTypeApiName == contentTypeApiName || permission.ContentTypeApiName == "*")
                && (permission.FieldApiName == field.FieldApiName || permission.FieldApiName == null)
                && (permission.Locale == field.Locale || permission.Locale == null))
            .ToListAsync(cancellationToken);

        return rolePermissions.Any(permission => permission.CanManageItems);
    }

    private async Task<bool> HasRolePermission(
        Guid userId,
        Func<RoleRecord, bool> check,
        CancellationToken cancellationToken)
    {
        var roleIds = RoleIdsFor(userId);

        var roles = await dbContext.Roles
            .AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);

        return roles.Any(check);
    }

    private IQueryable<Guid> RoleIdsFor(Guid userId)
    {
        var directRoleIds = dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.RoleId);
        var groupRoleIds =
            from userGroup in dbContext.UserGroups.AsNoTracking()
            join groupRole in dbContext.GroupRoles.AsNoTracking()
                on userGroup.GroupId equals groupRole.GroupId
            where userGroup.UserId == userId
            select groupRole.RoleId;

        return directRoleIds.Concat(groupRoleIds);
    }

    private static bool HasUserPermission(
        ContentTypePermissionRecord permission,
        PermissionOperation operation)
        => operation switch
        {
            PermissionOperation.ManageSchema => permission.CanManageSchema,
            PermissionOperation.ManageItems => permission.CanManageItems,
            PermissionOperation.ReadItems => permission.CanReadItems,
            _ => false
        };

    private static bool HasRolePermission(
        RoleContentTypePermissionRecord permission,
        PermissionOperation operation)
        => operation switch
        {
            PermissionOperation.ManageSchema => permission.CanManageSchema,
            PermissionOperation.ManageItems => permission.CanManageItems,
            PermissionOperation.ReadItems => permission.CanReadItems,
            _ => false
        };

    private enum PermissionOperation
    {
        ManageSchema,
        ManageItems,
        ReadItems
    }
}
