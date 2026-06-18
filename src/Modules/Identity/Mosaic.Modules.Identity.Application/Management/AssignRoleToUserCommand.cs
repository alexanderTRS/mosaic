namespace Mosaic.Modules.Identity.Application.Management;

public sealed record AssignRoleToUserCommand(Guid UserId, Guid RoleId);
