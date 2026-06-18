namespace Mosaic.Modules.Identity.Application.Management;

public sealed record CreateServiceAccountCommand(
    string Name,
    string DisplayName,
    bool CanViewGraphQLSchema);
