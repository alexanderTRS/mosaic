namespace Mosaic.Modules.Identity.Domain;

public sealed record AuthProvider
{
    private AuthProvider(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AuthProvider Local { get; } = new("local");

    public static AuthProvider From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Authentication provider is required.", nameof(value));
        }

        return new AuthProvider(value.Trim().ToLowerInvariant());
    }

    public override string ToString() => Value;
}
