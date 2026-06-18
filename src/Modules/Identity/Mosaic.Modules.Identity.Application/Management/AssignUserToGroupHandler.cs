using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class AssignUserToGroupHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IAuditLog auditLog;

    public AssignUserToGroupHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.auditLog = auditLog;
    }

    public async Task<UserDetails> Handle(AssignUserToGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        var user = await repository.GetUser(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User '{command.UserId}' was not found.");
        _ = await repository.GetGroup(command.GroupId, cancellationToken)
            ?? throw new InvalidOperationException($"Group '{command.GroupId}' was not found.");

        await repository.AssignUserToGroup(command.UserId, command.GroupId, cancellationToken);
        await auditLog.Record(
            AuditAction.UserAssignedToGroup,
            $"{command.UserId}:{command.GroupId}",
            null,
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return user;
    }
}
