using FluentAssertions;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Application.Management;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Tests.Management;

public sealed class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_should_create_user_and_save()
    {
        var repo = new FakeRepo();
        var handler = BuildHandler(repo, adminCaller: true);

        var result = await handler.Handle(
            new CreateUserCommand("editor", "Editor1234!", isAdministrator: false, canViewGraphQLSchema: false),
            CancellationToken.None);

        result.UserName.Should().Be("editor");
        result.IsAdministrator.Should().BeFalse();
        repo.AddedUser.Should().NotBeNull();
        repo.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_should_throw_when_caller_is_not_admin()
    {
        var handler = BuildHandler(new FakeRepo(), adminCaller: false);

        var act = async () => await handler.Handle(
            new CreateUserCommand("editor", "Editor1234!", false, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task Handle_should_throw_when_caller_is_unauthenticated()
    {
        var handler = BuildHandler(new FakeRepo(), adminCaller: false, authenticated: false);

        var act = async () => await handler.Handle(
            new CreateUserCommand("editor", "Editor1234!", false, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task Handle_should_throw_when_username_already_exists()
    {
        var repo = new FakeRepo(userExists: true);
        var handler = BuildHandler(repo, adminCaller: true);

        var act = async () => await handler.Handle(
            new CreateUserCommand("admin", "Admin1234!", false, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_should_throw_when_password_policy_violated()
    {
        var handler = BuildHandler(new FakeRepo(), adminCaller: true, weakPassword: true);

        var act = async () => await handler.Handle(
            new CreateUserCommand("editor", "weak", false, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_should_hash_password_before_storing()
    {
        var repo = new FakeRepo();
        var handler = BuildHandler(repo, adminCaller: true);

        await handler.Handle(
            new CreateUserCommand("editor", "Editor1234!", false, false),
            CancellationToken.None);

        repo.StoredPasswordHash.Should().StartWith("hashed:");
    }

    [Fact]
    public async Task Handle_should_grant_can_view_schema_when_administrator()
    {
        var repo = new FakeRepo();
        var handler = BuildHandler(repo, adminCaller: true);

        var result = await handler.Handle(
            new CreateUserCommand("superadmin", "Admin1234!", isAdministrator: true, canViewGraphQLSchema: false),
            CancellationToken.None);

        result.CanViewGraphQLSchema.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_should_record_audit_event()
    {
        var repo = new FakeRepo();
        var auditLog = new RecordingAuditLog();
        var handler = BuildHandler(repo, adminCaller: true, auditLog: auditLog);

        await handler.Handle(
            new CreateUserCommand("editor", "Editor1234!", false, false),
            CancellationToken.None);

        auditLog.RecordedAction.Should().Be(AuditAction.UserCreated);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CreateUserHandler BuildHandler(
        FakeRepo repo,
        bool adminCaller,
        bool authenticated = true,
        bool weakPassword = false,
        IAuditLog? auditLog = null)
    {
        var currentUser = new CurrentUser(
            Guid.NewGuid(), adminCaller ? "admin" : "user",
            IsAuthenticated: authenticated,
            IsAdministrator: adminCaller,
            CanViewGraphQLSchema: false);
        return new CreateUserHandler(
            new FixedCurrentUserAccessor(currentUser),
            repo,
            new FakePasswordHasher(),
            weakPassword ? new RejectingPasswordPolicy() : new AcceptingPasswordPolicy(),
            auditLog ?? new NullAuditLog());
    }

    private sealed class FakeRepo : IIdentityManagementRepository
    {
        private readonly bool userExists;
        public FakeRepo(bool userExists = false) => this.userExists = userExists;
        public UserDetails? AddedUser { get; private set; }
        public string? StoredPasswordHash { get; private set; }
        public bool SaveChangesCalled { get; private set; }
        public Task<bool> ExistsByUserName(string name, CancellationToken ct) => Task.FromResult(userExists);
        public Task<UserDetails?> GetUser(Guid id, CancellationToken ct) => Task.FromResult<UserDetails?>(null);
        public Task<RoleDetails?> GetRole(Guid id, CancellationToken ct) => Task.FromResult<RoleDetails?>(null);
        public Task<GroupDetails?> GetGroup(Guid id, CancellationToken ct) => Task.FromResult<GroupDetails?>(null);
        public Task AddUser(UserDetails user, string hash, CancellationToken ct) { AddedUser = user; StoredPasswordHash = hash; return Task.CompletedTask; }
        public Task AddRole(RoleDetails role, CancellationToken ct) => Task.CompletedTask;
        public Task AddGroup(GroupDetails group, CancellationToken ct) => Task.CompletedTask;
        public Task GrantContentTypeAccess(Guid userId, ContentTypePermissionGrant p, CancellationToken ct) => Task.CompletedTask;
        public Task GrantRoleContentTypeAccess(Guid roleId, ContentTypePermissionGrant p, CancellationToken ct) => Task.CompletedTask;
        public Task AssignRoleToUser(Guid userId, Guid roleId, CancellationToken ct) => Task.CompletedTask;
        public Task AssignUserToGroup(Guid userId, Guid groupId, CancellationToken ct) => Task.CompletedTask;
        public Task AssignRoleToGroup(Guid groupId, Guid roleId, CancellationToken ct) => Task.CompletedTask;
        public Task AddServiceAccountToken(Guid saId, string hash, string name, DateTimeOffset exp, CancellationToken ct) => Task.CompletedTask;
        public Task SaveChanges(CancellationToken ct) { SaveChangesCalled = true; return Task.CompletedTask; }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    private sealed class AcceptingPasswordPolicy : IPasswordPolicy
    {
        public void EnsureValid(string password) { }
    }

    private sealed class RejectingPasswordPolicy : IPasswordPolicy
    {
        public void EnsureValid(string password) => throw new InvalidOperationException("Password too weak.");
    }

    private sealed class FixedCurrentUserAccessor : ICurrentUserAccessor
    {
        public FixedCurrentUserAccessor(CurrentUser user) => CurrentUser = user;
        public CurrentUser CurrentUser { get; }
    }

    private sealed class RecordingAuditLog : IAuditLog
    {
        public string? RecordedAction { get; private set; }
        public Task Record(string action, string subject, string? details, CancellationToken ct)
        {
            RecordedAction = action;
            return Task.CompletedTask;
        }
    }

    private sealed class NullAuditLog : IAuditLog
    {
        public Task Record(string action, string subject, string? details, CancellationToken ct) => Task.CompletedTask;
    }
}
