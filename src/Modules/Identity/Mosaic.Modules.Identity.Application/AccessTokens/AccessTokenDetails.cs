namespace Mosaic.Modules.Identity.Application.AccessTokens;

public sealed record AccessTokenDetails(
    Guid UserId,
    string UserName,
    bool IsAdministrator,
    bool CanViewGraphQLSchema);
