using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class AssignRoleToUserHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IAuditLog auditLog;

    public AssignRoleToUserHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.auditLog = auditLog;
    }

    public async Task<UserDetails> Handle(AssignRoleToUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        var user = await repository.GetUser(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User '{command.UserId}' was not found.");
        _ = await repository.GetRole(command.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{command.RoleId}' was not found.");

        await repository.AssignRoleToUser(command.UserId, command.RoleId, cancellationToken);
        await auditLog.Record(
            AuditAction.RoleAssignedToUser,
            $"{command.UserId}:{command.RoleId}",
            null,
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return user;
    }
}
