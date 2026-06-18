namespace Mosaic.Modules.Identity.Application.Management;

public sealed record AssignRoleToGroupCommand(Guid GroupId, Guid RoleId);
