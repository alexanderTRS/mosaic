using System.Text.Json;
using System.Text.RegularExpressions;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.ContentFields;

public sealed record ContentFieldSettings
{
    private ContentFieldSettings(
        int? minLength,
        int? maxLength,
        string? regexPattern,
        decimal? minNumber,
        decimal? maxNumber,
        IReadOnlyCollection<string> requiredLocales,
        bool isUnique,
        bool isRepeatable,
        string? defaultValue,
        string? relationTargetContentTypeApiName)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        RegexPattern = regexPattern;
        MinNumber = minNumber;
        MaxNumber = maxNumber;
        RequiredLocales = requiredLocales;
        IsUnique = isUnique;
        IsRepeatable = isRepeatable;
        DefaultValue = defaultValue;
        RelationTargetContentTypeApiName = relationTargetContentTypeApiName;
    }

    public static ContentFieldSettings Empty { get; } = Create();

    public int? MinLength { get; }

    public int? MaxLength { get; }

    public string? RegexPattern { get; }

    public decimal? MinNumber { get; }

    public decimal? MaxNumber { get; }

    public IReadOnlyCollection<string> RequiredLocales { get; }

    public bool IsUnique { get; }

    public bool IsRepeatable { get; }

    public string? DefaultValue { get; }

    public string? RelationTargetContentTypeApiName { get; }

    public static ContentFieldSettings Create(
        int? minLength = null,
        int? maxLength = null,
        string? regexPattern = null,
        decimal? minNumber = null,
        decimal? maxNumber = null,
        IReadOnlyCollection<string>? requiredLocales = null,
        bool isUnique = false,
        bool isRepeatable = false,
        string? defaultValue = null,
        string? relationTargetContentTypeApiName = null)
    {
        if (minLength < 0)
        {
            throw new DomainRuleViolationException("Minimum length cannot be negative.");
        }

        if (maxLength < 0)
        {
            throw new DomainRuleViolationException("Maximum length cannot be negative.");
        }

        if (minLength is not null && maxLength is not null && minLength > maxLength)
        {
            throw new DomainRuleViolationException("Minimum length cannot be greater than maximum length.");
        }

        if (minNumber is not null && maxNumber is not null && minNumber > maxNumber)
        {
            throw new DomainRuleViolationException("Minimum number cannot be greater than maximum number.");
        }

        if (!string.IsNullOrWhiteSpace(regexPattern))
        {
            _ = new Regex(regexPattern, RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }

        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            _ = JsonDocument.Parse(defaultValue);
        }

        if (!string.IsNullOrWhiteSpace(relationTargetContentTypeApiName))
        {
            _ = ApiName.From(relationTargetContentTypeApiName);
        }

        return new ContentFieldSettings(
            minLength,
            maxLength,
            string.IsNullOrWhiteSpace(regexPattern) ? null : regexPattern,
            minNumber,
            maxNumber,
            requiredLocales?
                .Select(locale => locale.Trim().ToLowerInvariant())
                .Where(locale => locale.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray() ?? [],
            isUnique,
            isRepeatable,
            string.IsNullOrWhiteSpace(defaultValue) ? null : defaultValue,
            string.IsNullOrWhiteSpace(relationTargetContentTypeApiName) ? null : relationTargetContentTypeApiName.Trim());
    }
}
