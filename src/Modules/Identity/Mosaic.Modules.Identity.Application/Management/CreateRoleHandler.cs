using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

public sealed class CreateRoleHandler
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IIdentityManagementRepository repository;
    private readonly IAuditLog auditLog;

    public CreateRoleHandler(
        ICurrentUserAccessor currentUserAccessor,
        IIdentityManagementRepository repository,
        IAuditLog auditLog)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
        this.auditLog = auditLog;
    }

    public async Task<RoleDetails> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ManagementAuthorization.EnsureAdministrator(currentUserAccessor);

        var role = ApplyPreset(new RoleDetails(
            Guid.NewGuid(),
            Normalize(command.Name),
            command.DisplayName.Trim(),
            command.Preset,
            command.CanCreateContentTypes,
            command.CanViewGraphQLSchema));

        await repository.AddRole(role, cancellationToken);
        await auditLog.Record(
            AuditAction.RoleCreated,
            role.Id.ToString(),
            $"Name={role.Name};Preset={role.Preset}",
            cancellationToken);
        await repository.SaveChanges(cancellationToken);

        return role;
    }

    private static RoleDetails ApplyPreset(RoleDetails role)
        => role.Preset switch
        {
            PermissionPreset.Administrator => role with { CanCreateContentTypes = true, CanViewGraphQLSchema = true },
            PermissionPreset.Developer => role with { CanCreateContentTypes = true, CanViewGraphQLSchema = true },
            PermissionPreset.ContentManager => role with { CanCreateContentTypes = false, CanViewGraphQLSchema = true },
            PermissionPreset.Editor => role with { CanCreateContentTypes = false, CanViewGraphQLSchema = false },
            PermissionPreset.Viewer => role with { CanCreateContentTypes = false, CanViewGraphQLSchema = false },
            _ => role
        };

    private static string Normalize(string value)
        => value.Trim();
}
