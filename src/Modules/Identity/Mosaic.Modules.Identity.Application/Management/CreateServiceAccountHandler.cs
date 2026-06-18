using Mosaic.Modules.Identity.Application.Login;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class CreateServiceAccountHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IAuditLog auditLog;

    public CreateServiceAccountHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IPasswordHasher passwordHasher,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.auditLog = auditLog;
    }

    public async Task<UserDetails> Handle(CreateServiceAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        if (await repository.ExistsByUserName(command.Name, cancellationToken))
        {
            throw new InvalidOperationException($"Service account '{command.Name}' already exists.");
        }

        var serviceAccount = new UserDetails(
            Guid.NewGuid(),
            command.Name.Trim(),
            IsAdministrator: false,
            command.CanViewGraphQLSchema,
            IsServiceAccount: true);

        await repository.AddUser(
            serviceAccount,
            passwordHasher.Hash(Guid.NewGuid().ToString("N")),
            cancellationToken);
        await auditLog.Record(
            AuditAction.ServiceAccountCreated,
            serviceAccount.Id.ToString(),
            $"Name={serviceAccount.UserName};DisplayName={command.DisplayName.Trim()}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return serviceAccount;
    }
}
