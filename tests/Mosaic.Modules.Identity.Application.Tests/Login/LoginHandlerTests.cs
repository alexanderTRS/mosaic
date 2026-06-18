using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Identity.Application.Tests.Login;

public sealed class LoginHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(60);

    [Fact]
    public async Task Handle_should_return_token_for_valid_credentials()
    {
        var userId = Guid.NewGuid();
        var repo = new FakeUserCredentialRepository(new UserCredentials(userId, "admin", "hash", isAdministrator: true, canViewGraphQLSchema: true));
        var hasher = new FakePasswordHasher(verifyResult: true);
        var handler = BuildHandler(repo, hasher);

        var result = await handler.Handle(new LoginCommand("admin", "Admin1234"), CancellationToken.None);

        result.UserName.Should().Be("admin");
        result.UserId.Should().Be(userId);
        result.IsAdministrator.Should().BeTrue();
        result.CanViewGraphQLSchema.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.AccessTokenExpiresAt.Should().Be(Now.Add(TokenLifetime));
    }

    [Fact]
    public async Task Handle_should_throw_when_user_not_found()
    {
        var repo = new FakeUserCredentialRepository(null);
        var handler = BuildHandler(repo, new FakePasswordHasher(verifyResult: true));

        var act = async () => await handler.Handle(new LoginCommand("unknown", "pass"), CancellationToken.None);
        await act.Should().ThrowAsync<LoginFailedException>();
    }

    [Fact]
    public async Task Handle_should_throw_when_password_is_wrong()
    {
        var repo = new FakeUserCredentialRepository(new UserCredentials(Guid.NewGuid(), "admin", "hash", false, false));
        var handler = BuildHandler(repo, new FakePasswordHasher(verifyResult: false));

        var act = async () => await handler.Handle(new LoginCommand("admin", "wrong"), CancellationToken.None);
        await act.Should().ThrowAsync<LoginFailedException>();
    }

    [Fact]
    public async Task Handle_should_save_access_token_to_repository()
    {
        var repo = new FakeUserCredentialRepository(new UserCredentials(Guid.NewGuid(), "admin", "hash", true, true));
        var handler = BuildHandler(repo, new FakePasswordHasher(verifyResult: true));

        await handler.Handle(new LoginCommand("admin", "Admin1234"), CancellationToken.None);

        repo.AddedTokenHash.Should().NotBeNullOrWhiteSpace();
        repo.AddedTokenExpiresAt.Should().Be(Now.Add(TokenLifetime));
        repo.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_should_record_audit_event_on_success()
    {
        var repo = new FakeUserCredentialRepository(new UserCredentials(Guid.NewGuid(), "admin", "hash", true, true));
        var auditLog = new RecordingAuditLog();
        var handler = BuildHandler(repo, new FakePasswordHasher(verifyResult: true), auditLog);

        await handler.Handle(new LoginCommand("admin", "Admin1234"), CancellationToken.None);

        auditLog.RecordedAction.Should().Be(AuditAction.LoginSucceeded);
    }

    [Fact]
    public async Task Handle_should_not_record_audit_event_on_failure()
    {
        var repo = new FakeUserCredentialRepository(null);
        var auditLog = new RecordingAuditLog();
        var handler = BuildHandler(repo, new FakePasswordHasher(verifyResult: true), auditLog);

        try { await handler.Handle(new LoginCommand("unknown", "pass"), CancellationToken.None); } catch { }

        auditLog.RecordedAction.Should().BeNull();
    }

    private LoginHandler BuildHandler(
        FakeUserCredentialRepository repo,
        FakePasswordHasher hasher,
        IAuditLog? auditLog = null)
        => new(repo, hasher, new FakeTokenGenerator(), auditLog ?? new NullAuditLog(),
            new FixedClock(Now), TokenLifetime, NullLogger<LoginHandler>.Instance);

    // ── Fakes ────────────────────────────────────────────────────────────────

    private sealed class FakeUserCredentialRepository : IUserCredentialRepository
    {
        private readonly UserCredentials? user;
        public FakeUserCredentialRepository(UserCredentials? user) => this.user = user;
        public string? AddedTokenHash { get; private set; }
        public DateTimeOffset AddedTokenExpiresAt { get; private set; }
        public bool SaveChangesCalled { get; private set; }
        public Task<UserCredentials?> GetByUserName(string name, CancellationToken ct) => Task.FromResult(user);
        public Task AddAccessToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct)
        {
            AddedTokenHash = tokenHash;
            AddedTokenExpiresAt = expiresAt;
            return Task.CompletedTask;
        }
        public Task SaveChanges(CancellationToken ct) { SaveChangesCalled = true; return Task.CompletedTask; }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        private readonly bool verifyResult;
        public FakePasswordHasher(bool verifyResult) => this.verifyResult = verifyResult;
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string hash) => verifyResult;
    }

    private sealed class FakeTokenGenerator : ITokenGenerator
    {
        public string Generate() => "test-token-value";
        public string Hash(string token) => $"hashed:{token}";
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

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
