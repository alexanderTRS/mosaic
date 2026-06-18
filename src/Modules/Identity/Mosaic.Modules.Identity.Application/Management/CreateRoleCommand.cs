namespace Mosaic.Modules.Identity.Application.Management;

public sealed record CreateRoleCommand(
    string Name,
    string DisplayName,
    PermissionPreset Preset,
    bool CanCreateContentTypes,
    bool CanViewGraphQLSchema);
