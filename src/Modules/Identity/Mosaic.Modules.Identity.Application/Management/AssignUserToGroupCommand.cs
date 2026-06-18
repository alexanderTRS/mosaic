namespace Mosaic.Modules.Identity.Application.Management;

public sealed record AssignUserToGroupCommand(Guid UserId, Guid GroupId);
