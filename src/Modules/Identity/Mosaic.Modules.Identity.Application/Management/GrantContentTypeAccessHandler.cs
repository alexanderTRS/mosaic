using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class GrantContentTypeAccessHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IAuditLog auditLog;

    public GrantContentTypeAccessHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.auditLog = auditLog;
    }

    public async Task<UserDetails> Handle(
        GrantContentTypeAccessCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureAdministrator();

        var user = await repository.GetUser(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User '{command.UserId}' was not found.");

        await repository.GrantContentTypeAccess(
            command.UserId,
            new ContentTypePermissionGrant(
                command.ContentTypeApiName,
                command.CanManageSchema,
                command.CanManageItems,
                command.CanReadItems,
                command.FieldApiName,
                command.Locale),
            cancellationToken);
        await auditLog.Record(
            AuditAction.ContentTypeAccessGranted,
            $"{command.UserId}:{command.ContentTypeApiName}",
            $"CanManageSchema={command.CanManageSchema};CanManageItems={command.CanManageItems};CanReadItems={command.CanReadItems};FieldApiName={command.FieldApiName};Locale={command.Locale}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return user;
    }

    private void EnsureAdministrator()
    {
        var user = currentUserAccessor.CurrentUser;
        if (!user.IsAuthenticated || !user.IsAdministrator)
        {
            throw new AccessDeniedException("Only administrators can grant content type access.");
        }
    }
}
