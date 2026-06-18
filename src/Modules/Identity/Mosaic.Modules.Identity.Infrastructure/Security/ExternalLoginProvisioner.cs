using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class ExternalLoginProvisioner
{
    private readonly IdentityDbContext dbContext;
    private readonly IPasswordHasher passwordHasher;
    private readonly IClock clock;

    public ExternalLoginProvisioner(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IClock clock)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.clock = clock;
    }

    public async Task<ExternalLoginUser?> FindOrProvision(
        string provider,
        string subject,
        string? email,
        string? displayName,
        bool autoProvision,
        IReadOnlyCollection<string> defaultRoleNames,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var externalIdentity = await dbContext.ExternalIdentities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                identity => identity.Provider == normalizedProvider
                    && identity.Subject == subject,
                cancellationToken);

        UserRecord? user;
        if (externalIdentity is not null)
        {
            user = await dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Id == externalIdentity.UserId, cancellationToken);

            return user is null ? null : await ToExternalLoginUser(user, cancellationToken);
        }

        if (!autoProvision)
        {
            return null;
        }

        user = new UserRecord
        {
            Id = Guid.NewGuid(),
            UserName = await UniqueUserName(UserNameFrom(email, displayName, subject), cancellationToken),
            PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString("N")),
            IsAdministrator = false,
            CanViewGraphQLSchema = false,
            IsServiceAccount = false
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.ExternalIdentities.AddAsync(
            new ExternalIdentityRecord
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = normalizedProvider,
                Subject = subject,
                Email = email,
                DisplayName = displayName,
                LinkedAt = clock.UtcNow
            },
            cancellationToken);

        if (defaultRoleNames.Count > 0)
        {
            var roles = await dbContext.Roles
                .Where(role => defaultRoleNames.Contains(role.Name))
                .ToListAsync(cancellationToken);
            foreach (var role in roles)
            {
                await dbContext.UserRoles.AddAsync(
                    new UserRoleRecord { UserId = user.Id, RoleId = role.Id },
                    cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await ToExternalLoginUser(user, cancellationToken);
    }

    private async Task<ExternalLoginUser> ToExternalLoginUser(
        UserRecord user,
        CancellationToken cancellationToken)
    {
        var canViewGraphQLSchema = user.IsAdministrator
            || user.CanViewGraphQLSchema
            || await HasRolePermission(user.Id, role => role.CanViewGraphQLSchema, cancellationToken);

        return new ExternalLoginUser(
            user.Id,
            user.UserName,
            user.IsAdministrator,
            canViewGraphQLSchema);
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

    private async Task<string> UniqueUserName(string baseUserName, CancellationToken cancellationToken)
    {
        var userName = baseUserName;
        var suffix = 1;
        while (await dbContext.Users.AnyAsync(user => user.UserName == userName, cancellationToken))
        {
            suffix++;
            userName = $"{baseUserName}-{suffix}";
        }

        return userName;
    }

    private static string UserNameFrom(string? email, string? displayName, string subject)
    {
        var value = FirstNonEmpty(email, displayName, subject)
            .Trim()
            .ToLowerInvariant();
        var sanitized = new string(value
            .Select(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-' or '@'
                ? character
                : '-')
            .ToArray())
            .Trim('-', '.', '_', '@');

        return string.IsNullOrWhiteSpace(sanitized)
            ? $"external-{Guid.NewGuid():N}"
            : sanitized;
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "external";
}
