namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed record ExternalLoginUser(
    Guid UserId,
    string UserName,
    bool IsAdministrator,
    bool CanViewGraphQLSchema);
