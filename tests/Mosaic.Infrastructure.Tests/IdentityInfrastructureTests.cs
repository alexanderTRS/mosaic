using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Identity.Application.Management;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.Modules.Identity.Infrastructure.Security;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Infrastructure.Tests;

public sealed class IdentityInfrastructureTests : IAsyncLifetime
{
    private InfrastructurePostgresDatabase database = null!;

    public async Task InitializeAsync()
    {
        database = new InfrastructurePostgresDatabase();
        await database.Initialize();
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
    }

    [Fact]
    public async Task AccessTokenService_should_validate_revoke_and_ignore_expired_tokens()
    {
        var userId = Guid.NewGuid();
        var tokenGenerator = new TokenGenerator();
        var validToken = "valid-token";
        var expiredToken = "expired-token";
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero));

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            await dbContext.Users.AddAsync(
                new UserRecord
                {
                    Id = userId,
                    UserName = "editor",
                    PasswordHash = "hash",
                    IsAdministrator = false,
                    CanViewGraphQLSchema = true
                });
            await dbContext.AccessTokens.AddRangeAsync(
                new AccessTokenRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = tokenGenerator.Hash(validToken),
                    Kind = "UserLogin",
                    CreatedAt = clock.UtcNow,
                    ExpiresAt = clock.UtcNow.AddMinutes(15)
                },
                new AccessTokenRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = tokenGenerator.Hash(expiredToken),
                    Kind = "UserLogin",
                    CreatedAt = clock.UtcNow.AddMinutes(-30),
                    ExpiresAt = clock.UtcNow.AddMinutes(-1)
                });
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var service = new AccessTokenService(dbContext, tokenGenerator, clock);

            var validDetails = await service.Validate(validToken, CancellationToken.None);
            validDetails.Should().NotBeNull();
            validDetails!.UserId.Should().Be(userId);
            validDetails.CanViewGraphQLSchema.Should().BeTrue();

            var expiredDetails = await service.Validate(expiredToken, CancellationToken.None);
            expiredDetails.Should().BeNull();

            await service.Revoke(tokenGenerator.Hash(validToken), CancellationToken.None);
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var service = new AccessTokenService(dbContext, tokenGenerator, clock);
            var revokedDetails = await service.Validate(validToken, CancellationToken.None);
            revokedDetails.Should().BeNull();
        }
    }

    [Fact]
    public async Task IdentityManagementRepository_should_upsert_content_type_permissions()
    {
        var user = new UserDetails(Guid.NewGuid(), "manager", IsAdministrator: false, CanViewGraphQLSchema: false);

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var repository = new IdentityManagementRepository(dbContext);
            await repository.AddUser(user, "hash", CancellationToken.None);
            await repository.SaveChanges(CancellationToken.None);

            await repository.GrantContentTypeAccess(
                user.Id,
                new ContentTypePermissionGrant(
                    "product",
                    CanManageSchema: true,
                    CanManageItems: false,
                    CanReadItems: true),
                CancellationToken.None);
            await repository.SaveChanges(CancellationToken.None);

            await repository.GrantContentTypeAccess(
                user.Id,
                new ContentTypePermissionGrant(
                    "product",
                    CanManageSchema: false,
                    CanManageItems: true,
                    CanReadItems: true),
                CancellationToken.None);
            await repository.SaveChanges(CancellationToken.None);
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var permissions = await dbContext.ContentTypePermissions.ToListAsync();

            permissions.Should().ContainSingle();
            permissions.Single().CanManageSchema.Should().BeFalse();
            permissions.Single().CanManageItems.Should().BeTrue();
            permissions.Single().CanReadItems.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ContentAccessService_should_allow_permissions_from_user_role_and_group_role()
    {
        var directUserId = Guid.NewGuid();
        var groupUserId = Guid.NewGuid();
        var directRoleId = Guid.NewGuid();
        var groupRoleId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            await dbContext.Users.AddRangeAsync(
                new UserRecord
                {
                    Id = directUserId,
                    UserName = "direct-role-user",
                    PasswordHash = "hash",
                    IsAdministrator = false,
                    CanViewGraphQLSchema = false
                },
                new UserRecord
                {
                    Id = groupUserId,
                    UserName = "group-role-user",
                    PasswordHash = "hash",
                    IsAdministrator = false,
                    CanViewGraphQLSchema = false
                });
            await dbContext.Roles.AddRangeAsync(
                new RoleRecord
                {
                    Id = directRoleId,
                    Name = "product-editor",
                    DisplayName = "Product Editor",
                    Preset = PermissionPreset.Editor.ToString(),
                    CanCreateContentTypes = false,
                    CanViewGraphQLSchema = false
                },
                new RoleRecord
                {
                    Id = groupRoleId,
                    Name = "catalog-viewer",
                    DisplayName = "Catalog Viewer",
                    Preset = PermissionPreset.Viewer.ToString(),
                    CanCreateContentTypes = false,
                    CanViewGraphQLSchema = false
                });
            await dbContext.Groups.AddAsync(
                new GroupRecord
                {
                    Id = groupId,
                    Name = "catalog-team",
                    DisplayName = "Catalog Team"
                });
            await dbContext.UserRoles.AddAsync(
                new UserRoleRecord { UserId = directUserId, RoleId = directRoleId });
            await dbContext.UserGroups.AddAsync(
                new UserGroupRecord { UserId = groupUserId, GroupId = groupId });
            await dbContext.GroupRoles.AddAsync(
                new GroupRoleRecord { GroupId = groupId, RoleId = groupRoleId });
            await dbContext.RoleContentTypePermissions.AddRangeAsync(
                new RoleContentTypePermissionRecord
                {
                    Id = Guid.NewGuid(),
                    RoleId = directRoleId,
                    ContentTypeApiName = "product",
                    CanManageSchema = false,
                    CanManageItems = true,
                    CanReadItems = true
                },
                new RoleContentTypePermissionRecord
                {
                    Id = Guid.NewGuid(),
                    RoleId = groupRoleId,
                    ContentTypeApiName = "category",
                    CanManageSchema = false,
                    CanManageItems = false,
                    CanReadItems = true
                });
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var accessService = new ContentAccessService(
                new FixedCurrentUserAccessor(
                    new CurrentUser(
                        directUserId,
                        "direct-role-user",
                        IsAuthenticated: true,
                        IsAdministrator: false,
                        CanViewGraphQLSchema: false)),
                dbContext);

            await accessService.EnsureCanManageContentItems("product", CancellationToken.None);
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var accessService = new ContentAccessService(
                new FixedCurrentUserAccessor(
                    new CurrentUser(
                        groupUserId,
                        "group-role-user",
                        IsAuthenticated: true,
                        IsAdministrator: false,
                        CanViewGraphQLSchema: false)),
                dbContext);

            await accessService.EnsureCanReadContentItems("category", CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExternalLoginProvisioner_should_auto_provision_and_reuse_external_identity()
    {
        var roleId = Guid.NewGuid();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero));

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            await dbContext.Roles.AddAsync(
                new RoleRecord
                {
                    Id = roleId,
                    Name = "external-developer",
                    DisplayName = "External Developer",
                    Preset = PermissionPreset.Developer.ToString(),
                    CanCreateContentTypes = true,
                    CanViewGraphQLSchema = true
                });
            await dbContext.SaveChangesAsync();
        }

        Guid userId;
        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var provisioner = new ExternalLoginProvisioner(
                dbContext,
                new Pbkdf2PasswordHasher(),
                clock);

            var user = await provisioner.FindOrProvision(
                "keycloak",
                "subject-1",
                "editor@example.test",
                "Editor Example",
                autoProvision: true,
                ["external-developer"],
                CancellationToken.None);

            user.Should().NotBeNull();
            user!.UserName.Should().Be("editor@example.test");
            user.CanViewGraphQLSchema.Should().BeTrue();
            userId = user.UserId;
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var provisioner = new ExternalLoginProvisioner(
                dbContext,
                new Pbkdf2PasswordHasher(),
                clock);

            var sameUser = await provisioner.FindOrProvision(
                "keycloak",
                "subject-1",
                "changed@example.test",
                "Changed",
                autoProvision: true,
                ["external-developer"],
                CancellationToken.None);

            sameUser.Should().NotBeNull();
            sameUser!.UserId.Should().Be(userId);
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            (await dbContext.Users.CountAsync()).Should().Be(1);
            (await dbContext.ExternalIdentities.CountAsync()).Should().Be(1);
            (await dbContext.UserRoles.CountAsync()).Should().Be(1);
        }
    }

    [Fact]
    public async Task EfAuditLog_should_persist_actor_action_and_subject()
    {
        var actorId = Guid.NewGuid();
        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var auditLog = new EfAuditLog(
                dbContext,
                new FixedCurrentUserAccessor(
                    new CurrentUser(
                        actorId,
                        "admin",
                        IsAuthenticated: true,
                        IsAdministrator: true,
                        CanViewGraphQLSchema: true)));

            await auditLog.Record(
                AuditAction.UserCreated,
                "target-user-id",
                "UserName=editor",
                CancellationToken.None);
        }

        await using (var dbContext = database.CreateIdentityDbContext())
        {
            var auditEvent = await dbContext.AuditEvents.SingleAsync();

            auditEvent.ActorUserId.Should().Be(actorId);
            auditEvent.ActorUserName.Should().Be("admin");
            auditEvent.Action.Should().Be(AuditAction.UserCreated);
            auditEvent.Subject.Should().Be("target-user-id");
            auditEvent.Details.Should().Be("UserName=editor");
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FixedCurrentUserAccessor : ICurrentUserAccessor
    {
        public FixedCurrentUserAccessor(CurrentUser currentUser)
        {
            CurrentUser = currentUser;
        }

        public CurrentUser CurrentUser { get; }
    }
}
