namespace Mosaic.Modules.Identity.Application.Management;

public sealed record RoleDetails(
    Guid Id,
    string Name,
    string DisplayName,
    PermissionPreset Preset,
    bool CanCreateContentTypes,
    bool CanViewGraphQLSchema);
