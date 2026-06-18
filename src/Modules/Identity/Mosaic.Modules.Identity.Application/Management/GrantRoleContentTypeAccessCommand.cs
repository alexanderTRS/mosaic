namespace Mosaic.Modules.Identity.Application.Management;

public sealed record GrantRoleContentTypeAccessCommand(
    Guid RoleId,
    string ContentTypeApiName,
    bool CanManageSchema,
    bool CanManageItems,
    bool CanReadItems,
    string? FieldApiName = null,
    string? Locale = null);
