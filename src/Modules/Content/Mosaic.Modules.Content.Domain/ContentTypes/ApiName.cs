using System.Text.RegularExpressions;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.ContentTypes;

public sealed partial record ApiName
{
    private ApiName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public string GraphQlTypeName => char.ToUpperInvariant(Value[0]) + Value[1..];

    public static ApiName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException("API name is required.");
        }

        var normalized = value.Trim();
        if (!ApiNamePattern().IsMatch(normalized))
        {
            throw new DomainRuleViolationException(
                "API name must be lower camel case and contain only letters, digits, or underscores.");
        }

        return new ApiName(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ApiNamePattern();
}
