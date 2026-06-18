namespace Mosaic.Modules.Identity.Application.Management;

public sealed record UserDetails(
    Guid Id,
    string UserName,
    bool IsAdministrator,
    bool CanViewGraphQLSchema,
    bool IsServiceAccount = false);
