using Mosaic.Modules.Identity.Application.Login;

namespace Mosaic.Modules.Identity.Application.AccessTokens;

public sealed class RevokeAccessTokenHandler
{
    private readonly IAccessTokenService accessTokenService;
    private readonly ITokenGenerator tokenGenerator;

    public RevokeAccessTokenHandler(IAccessTokenService accessTokenService, ITokenGenerator tokenGenerator)
    {
        this.accessTokenService = accessTokenService;
        this.tokenGenerator = tokenGenerator;
    }

    public Task Handle(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.CompletedTask;
        }

        return accessTokenService.Revoke(tokenGenerator.Hash(token), cancellationToken);
    }
}
