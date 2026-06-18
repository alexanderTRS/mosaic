using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.ContentFields;

public sealed class ContentField : Entity<ContentFieldId>
{
    private ContentField(
        ContentFieldId id,
        ApiName apiName,
        string displayName,
        FieldKind kind,
        LocalizationMode localization,
        bool isRequired,
        ContentFieldSettings settings,
        bool isDeprecated)
        : base(id)
    {
        ApiName = apiName;
        DisplayName = displayName;
        Kind = kind;
        Localization = localization;
        IsRequired = isRequired;
        Settings = settings;
        IsDeprecated = isDeprecated;
    }

    public ApiName ApiName { get; }

    public string DisplayName { get; }

    public FieldKind Kind { get; }

    public LocalizationMode Localization { get; }

    public bool IsRequired { get; }

    public ContentFieldSettings Settings { get; }

    public bool IsDeprecated { get; private set; }

    public bool IsLocalized => Localization == LocalizationMode.Localized;

    public static ContentField Create(
        string apiName,
        string displayName,
        FieldKind kind,
        LocalizationMode localization,
        bool isRequired,
        ContentFieldSettings? settings = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainRuleViolationException("Content field display name is required.");
        }

        settings ??= ContentFieldSettings.Empty;
        EnsureSettingsMatchKind(kind, settings);

        return new ContentField(
            ContentFieldId.New(),
            ApiName.From(apiName),
            displayName.Trim(),
            kind,
            localization,
            isRequired,
            settings,
            isDeprecated: false);
    }

    public static ContentField Restore(
        ContentFieldId id,
        string apiName,
        string displayName,
        FieldKind kind,
        LocalizationMode localization,
        bool isRequired,
        ContentFieldSettings? settings = null,
        bool isDeprecated = false)
        => new(
            id,
            ApiName.From(apiName),
            displayName,
            kind,
            localization,
            isRequired,
            settings ?? ContentFieldSettings.Empty,
            isDeprecated);

    public void Deprecate()
    {
        IsDeprecated = true;
    }

    private static void EnsureSettingsMatchKind(FieldKind kind, ContentFieldSettings settings)
    {
        var hasStringRules = settings.MinLength is not null
            || settings.MaxLength is not null
            || settings.RegexPattern is not null;
        var hasNumberRules = settings.MinNumber is not null || settings.MaxNumber is not null;

        if (hasStringRules && kind is not (FieldKind.String or FieldKind.Text))
        {
            throw new DomainRuleViolationException("Length and regex validation can be used only for string or text fields.");
        }

        if (hasNumberRules && kind is not (FieldKind.Integer or FieldKind.Decimal))
        {
            throw new DomainRuleViolationException("Numeric validation can be used only for integer or decimal fields.");
        }

        if (settings.RelationTargetContentTypeApiName is not null && kind != FieldKind.Relation)
        {
            throw new DomainRuleViolationException("Relation target can be configured only for relation fields.");
        }
    }
}
