using Mosaic.Modules.Identity.Application.Login;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class CreateUserHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IPasswordPolicy passwordPolicy;
    private readonly IAuditLog auditLog;

    public CreateUserHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.passwordPolicy = passwordPolicy;
        this.auditLog = auditLog;
    }

    public async Task<UserDetails> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureAdministrator();
        passwordPolicy.EnsureValid(command.Password);

        if (await repository.ExistsByUserName(command.UserName, cancellationToken))
        {
            throw new InvalidOperationException($"User '{command.UserName}' already exists.");
        }

        var user = new UserDetails(
            Guid.NewGuid(),
            command.UserName.Trim(),
            command.IsAdministrator,
            command.IsAdministrator || command.CanViewGraphQLSchema);
        await repository.AddUser(user, passwordHasher.Hash(command.Password), cancellationToken);
        await auditLog.Record(
            AuditAction.UserCreated,
            user.Id.ToString(),
            $"UserName={user.UserName};IsAdministrator={user.IsAdministrator};CanViewGraphQLSchema={user.CanViewGraphQLSchema}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return user;
    }

    private void EnsureAdministrator()
    {
        var user = currentUserAccessor.CurrentUser;
        if (!user.IsAuthenticated || !user.IsAdministrator)
        {
            throw new AccessDeniedException("Only administrators can manage users.");
        }
    }
}
