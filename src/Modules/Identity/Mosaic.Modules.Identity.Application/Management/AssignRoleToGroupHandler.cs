using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class AssignRoleToGroupHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IAuditLog auditLog;

    public AssignRoleToGroupHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.auditLog = auditLog;
    }

    public async Task<GroupDetails> Handle(AssignRoleToGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        var group = await repository.GetGroup(command.GroupId, cancellationToken)
            ?? throw new InvalidOperationException($"Group '{command.GroupId}' was not found.");
        _ = await repository.GetRole(command.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{command.RoleId}' was not found.");

        await repository.AssignRoleToGroup(command.GroupId, command.RoleId, cancellationToken);
        await auditLog.Record(
            AuditAction.RoleAssignedToGroup,
            $"{command.GroupId}:{command.RoleId}",
            null,
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return group;
    }
}
