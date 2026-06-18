namespace Mosaic.Modules.Identity.Application.Login;

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid UserId,
    string UserName,
    bool IsAdministrator,
    bool CanViewGraphQLSchema);
