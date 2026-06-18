using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Identity.Application.Login;

namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class UserCredentialRepository : IUserCredentialRepository
{
    private readonly IdentityDbContext dbContext;

    public UserCredentialRepository(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UserCredentials?> GetByUserName(string userName, CancellationToken cancellationToken)
    {
        var normalized = userName.Trim();
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.UserName == normalized
                    && !user.IsServiceAccount,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var canViewGraphQLSchema = user.IsAdministrator
            || user.CanViewGraphQLSchema
            || await HasRolePermission(user.Id, role => role.CanViewGraphQLSchema, cancellationToken);

        return new UserCredentials(
            user.Id,
            user.UserName,
            user.PasswordHash,
            user.IsAdministrator,
            canViewGraphQLSchema);
    }

    public async Task AddAccessToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await dbContext.AccessTokens.AddAsync(
            new AccessTokenRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                Kind = "UserLogin",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = expiresAt
            },
            cancellationToken);
    }

    public Task SaveChanges(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    private async Task<bool> HasRolePermission(
        Guid userId,
        Func<RoleRecord, bool> check,
        CancellationToken cancellationToken)
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
        var roleIds = directRoleIds.Concat(groupRoleIds);

        var roles = await dbContext.Roles
            .AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);

        return roles.Any(check);
    }
}
