namespace Mosaic.Modules.Identity.Application.Login;

public sealed record UserCredentials(
    Guid Id,
    string UserName,
    string PasswordHash,
    bool IsAdministrator,
    bool CanViewGraphQLSchema);
