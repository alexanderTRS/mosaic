using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Identity.Application.AccessTokens;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class AccessTokenService : IAccessTokenService
{
    private readonly IdentityDbContext dbContext;
    private readonly ITokenGenerator tokenGenerator;
    private readonly IClock clock;

    public AccessTokenService(IdentityDbContext dbContext, ITokenGenerator tokenGenerator, IClock clock)
    {
        this.dbContext = dbContext;
        this.tokenGenerator = tokenGenerator;
        this.clock = clock;
    }

    public async Task<AccessTokenDetails?> Validate(string token, CancellationToken cancellationToken)
    {
        var tokenHash = tokenGenerator.Hash(token);
        var query =
            from accessToken in dbContext.AccessTokens.AsNoTracking()
            join userRecord in dbContext.Users.AsNoTracking()
                on accessToken.UserId equals userRecord.Id
            where accessToken.TokenHash == tokenHash
                && accessToken.RevokedAt == null
                && accessToken.ExpiresAt > clock.UtcNow
            select userRecord;

        var user = await query.SingleOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        var canViewGraphQLSchema = user.IsAdministrator
            || user.CanViewGraphQLSchema
            || await HasRolePermission(user.Id, role => role.CanViewGraphQLSchema, cancellationToken);

        return new AccessTokenDetails(
            user.Id,
            user.UserName,
            user.IsAdministrator,
            canViewGraphQLSchema);
    }

    public async Task Revoke(string tokenHash, CancellationToken cancellationToken)
    {
        var accessToken = await dbContext.AccessTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (accessToken is null || accessToken.RevokedAt is not null)
        {
            return;
        }

        accessToken.RevokedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

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
