namespace Mosaic.Modules.Identity.Application.AccessTokens;

public interface IAccessTokenService
{
    Task<AccessTokenDetails?> Validate(string token, CancellationToken cancellationToken);

    Task Revoke(string tokenHash, CancellationToken cancellationToken);
}
