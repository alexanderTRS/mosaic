namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class OidcIdentityOptions
{
    public bool Enabled { get; set; }

    public string Scheme { get; set; } = "MosaicOidc";

    public string Provider { get; set; } = "keycloak";

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string CallbackPath { get; set; } = "/signin-oidc";

    public bool RequireHttpsMetadata { get; set; } = true;

    public bool AutoProvisionUsers { get; set; } = true;

    public string[] DefaultRoleNames { get; set; } = [];
}
