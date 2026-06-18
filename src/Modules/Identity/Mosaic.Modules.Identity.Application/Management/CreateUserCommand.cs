namespace Mosaic.Modules.Identity.Application.Management;

public sealed record CreateUserCommand(
    string UserName,
    string Password,
    bool IsAdministrator,
    bool CanViewGraphQLSchema);
