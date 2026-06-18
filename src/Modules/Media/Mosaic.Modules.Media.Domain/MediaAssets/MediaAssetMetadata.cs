using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Media.Domain.MediaAssets;

public sealed class MediaAssetMetadata
{
    public MediaAssetMetadata(string? altText, IReadOnlyDictionary<string, string> localizedAltText)
    {
        AltText = NormalizeOptional(altText, 512);
        LocalizedAltText = localizedAltText
            .ToDictionary(
                item => NormalizeLocale(item.Key),
                item => NormalizeRequired(item.Value, nameof(localizedAltText), 512),
                StringComparer.OrdinalIgnoreCase);
    }

    public string? AltText { get; }

    public IReadOnlyDictionary<string, string> LocalizedAltText { get; }

    public static MediaAssetMetadata Empty => new(null, new Dictionary<string, string>());

    private static string NormalizeLocale(string locale)
    {
        var value = NormalizeRequired(locale, nameof(locale), 32).ToLowerInvariant();
        if (!value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_'))
        {
            throw new DomainRuleViolationException("Locale can contain only letters, digits, '-' and '_'.");
        }

        return value;
    }

    private static string NormalizeRequired(string value, string name, int maxLength)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainRuleViolationException($"{name} cannot be empty.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainRuleViolationException($"{name} cannot be longer than {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleViolationException($"Alt text cannot be longer than {maxLength} characters.");
        }

        return normalized;
    }
}
