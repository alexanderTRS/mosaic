using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class GrantRoleContentTypeAccessHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IAuditLog auditLog;

    public GrantRoleContentTypeAccessHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.auditLog = auditLog;
    }

    public async Task<RoleDetails> Handle(GrantRoleContentTypeAccessCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        var role = await repository.GetRole(command.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{command.RoleId}' was not found.");

        await repository.GrantRoleContentTypeAccess(
            command.RoleId,
            new ContentTypePermissionGrant(
                command.ContentTypeApiName,
                command.CanManageSchema,
                command.CanManageItems,
                command.CanReadItems,
                command.FieldApiName,
                command.Locale),
            cancellationToken);
        await auditLog.Record(
            AuditAction.RoleContentTypeAccessGranted,
            $"{command.RoleId}:{command.ContentTypeApiName}",
            $"CanManageSchema={command.CanManageSchema};CanManageItems={command.CanManageItems};CanReadItems={command.CanReadItems};FieldApiName={command.FieldApiName};Locale={command.Locale}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return role;
    }
}
