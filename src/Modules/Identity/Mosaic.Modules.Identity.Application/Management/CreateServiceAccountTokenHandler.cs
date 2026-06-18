using Mosaic.Modules.Identity.Application.Login;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class CreateServiceAccountTokenHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly ITokenGenerator tokenGenerator;
    private readonly IClock clock;
    private readonly IAuditLog auditLog;

    public CreateServiceAccountTokenHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        ITokenGenerator tokenGenerator,
        IClock clock,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.tokenGenerator = tokenGenerator;
        this.clock = clock;
        this.auditLog = auditLog;
    }

    public async Task<ServiceAccountTokenDetails> Handle(
        CreateServiceAccountTokenCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        var serviceAccount = await repository.GetUser(command.ServiceAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Service account '{command.ServiceAccountId}' was not found.");
        if (!serviceAccount.IsServiceAccount)
        {
            throw new InvalidOperationException($"User '{command.ServiceAccountId}' is not a service account.");
        }

        var lifetimeDays = Math.Clamp(command.LifetimeDays, 1, 3660);
        var token = tokenGenerator.Generate();
        var expiresAt = clock.UtcNow.AddDays(lifetimeDays);

        await repository.AddServiceAccountToken(
            command.ServiceAccountId,
            tokenGenerator.Hash(token),
            command.Name.Trim(),
            expiresAt,
            cancellationToken);
        await auditLog.Record(
            AuditAction.ServiceAccountTokenCreated,
            command.ServiceAccountId.ToString(),
            $"Name={command.Name.Trim()};ExpiresAt={expiresAt:O}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return new ServiceAccountTokenDetails(
            token,
            expiresAt,
            command.ServiceAccountId,
            command.Name.Trim());
    }
}
