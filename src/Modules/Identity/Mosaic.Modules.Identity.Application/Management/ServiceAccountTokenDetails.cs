namespace Mosaic.Modules.Identity.Application.Management;

public sealed record ServiceAccountTokenDetails(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid ServiceAccountId,
    string Name);
