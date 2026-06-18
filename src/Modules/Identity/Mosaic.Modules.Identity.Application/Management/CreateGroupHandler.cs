using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class CreateGroupHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IAuditLog auditLog;

    public CreateGroupHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.auditLog = auditLog;
    }

    public async Task<GroupDetails> Handle(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        var group = new GroupDetails(
            Guid.NewGuid(),
            command.Name.Trim(),
            command.DisplayName.Trim());
        await repository.AddGroup(group, cancellationToken);
        await auditLog.Record(
            AuditAction.GroupCreated,
            group.Id.ToString(),
            $"Name={group.Name}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return group;
    }
}
