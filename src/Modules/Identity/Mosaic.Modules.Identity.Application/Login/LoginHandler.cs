using Microsoft.Extensions.Logging;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Identity.Application.Login;

public sealed class LoginHandler
{
    private readonly IUserCredentialRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenGenerator tokenGenerator;
    private readonly IAuditLog auditLog;
    private readonly IClock clock;
    private readonly TimeSpan accessTokenLifetime;
    private readonly ILogger<LoginHandler> logger;

    public LoginHandler(
        IUserCredentialRepository repository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IAuditLog auditLog,
        IClock clock,
        TimeSpan accessTokenLifetime,
        ILogger<LoginHandler> logger)
    {
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.tokenGenerator = tokenGenerator;
        this.auditLog = auditLog;
        this.clock = clock;
        this.accessTokenLifetime = accessTokenLifetime;
        this.logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await repository.GetByUserName(command.UserName, cancellationToken)
            ?? throw new LoginFailedException();

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new LoginFailedException();
        }

        var token = tokenGenerator.Generate();
        var expiresAt = clock.UtcNow.Add(accessTokenLifetime);
        await repository.AddAccessToken(user.Id, tokenGenerator.Hash(token), expiresAt, cancellationToken);
        await auditLog.Record(
            AuditAction.LoginSucceeded,
            user.Id.ToString(),
            $"UserName={user.UserName}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        logger.LogInformation("User {UserName} logged in", user.UserName);

        return new LoginResult(
            token,
            expiresAt,
            user.Id,
            user.UserName,
            user.IsAdministrator,
            user.CanViewGraphQLSchema);
    }
}
