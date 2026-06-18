namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class IdentitySecurityOptions
{
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    public int MinimumPasswordLength { get; init; } = 8;

    public bool RequirePasswordDigit { get; init; } = true;

    public bool RequirePasswordUppercase { get; init; } = true;
}
