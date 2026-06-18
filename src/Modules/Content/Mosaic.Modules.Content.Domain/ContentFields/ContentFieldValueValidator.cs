using System.Text.Json;
using System.Text.RegularExpressions;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.ContentFields;

public static class ContentFieldValueValidator
{
    public static void Validate(ContentField field, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        if (field.Settings.IsRepeatable)
        {
            if (value.ValueKind != JsonValueKind.Array)
            {
                throw new DomainRuleViolationException($"Field '{field.ApiName}' must be an array.");
            }

            foreach (var item in value.EnumerateArray())
            {
                ValidateSingle(field, item);
            }

            return;
        }

        ValidateSingle(field, value);
    }

    private static void ValidateSingle(ContentField field, JsonElement value)
    {
        if (field.IsLocalized)
        {
            ValidateLocalized(field, value);
            return;
        }

        ValidateKind(field, value);
    }

    private static void ValidateLocalized(ContentField field, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new DomainRuleViolationException($"Localized field '{field.ApiName}' must be a JSON object.");
        }

        foreach (var locale in field.Settings.RequiredLocales)
        {
            if (!value.TryGetProperty(locale, out var localizedValue)
                || localizedValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new DomainRuleViolationException(
                    $"Localized field '{field.ApiName}' requires locale '{locale}'.");
            }
        }

        foreach (var localizedValue in value.EnumerateObject())
        {
            ValidateKind(field, localizedValue.Value);
        }
    }

    private static void ValidateKind(ContentField field, JsonElement value)
    {
        switch (field.Kind)
        {
            case FieldKind.String:
            case FieldKind.Text:
                ValidateString(field, value);
                break;
            case FieldKind.Boolean:
                RequireKind(field, value, JsonValueKind.True, JsonValueKind.False);
                break;
            case FieldKind.Integer:
                ValidateInteger(field, value);
                break;
            case FieldKind.Decimal:
                ValidateDecimal(field, value);
                break;
            case FieldKind.DateTime:
                ValidateDateTime(field, value);
                break;
            case FieldKind.Json:
                break;
            case FieldKind.Media:
            case FieldKind.Relation:
                ValidateReference(field, value);
                break;
            default:
                throw new DomainRuleViolationException($"Unsupported field kind '{field.Kind}'.");
        }
    }

    private static void ValidateString(ContentField field, JsonElement value)
    {
        RequireKind(field, value, JsonValueKind.String);
        var text = value.GetString() ?? string.Empty;

        if (field.Settings.MinLength is not null && text.Length < field.Settings.MinLength)
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' is shorter than {field.Settings.MinLength} characters.");
        }

        if (field.Settings.MaxLength is not null && text.Length > field.Settings.MaxLength)
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' is longer than {field.Settings.MaxLength} characters.");
        }

        if (field.Settings.RegexPattern is not null
            && !Regex.IsMatch(text, field.Settings.RegexPattern, RegexOptions.None, TimeSpan.FromMilliseconds(250)))
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' does not match the configured pattern.");
        }
    }

    private static void ValidateInteger(ContentField field, JsonElement value)
    {
        RequireKind(field, value, JsonValueKind.Number);
        if (!value.TryGetInt64(out var number))
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' must be an integer.");
        }

        ValidateNumberRange(field, number);
    }

    private static void ValidateDecimal(ContentField field, JsonElement value)
    {
        RequireKind(field, value, JsonValueKind.Number);
        if (!value.TryGetDecimal(out var number))
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' must be a decimal number.");
        }

        ValidateNumberRange(field, number);
    }

    private static void ValidateDateTime(ContentField field, JsonElement value)
    {
        RequireKind(field, value, JsonValueKind.String);
        if (!DateTimeOffset.TryParse(value.GetString(), out _))
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' must be an ISO date-time string.");
        }
    }

    private static void ValidateReference(ContentField field, JsonElement value)
    {
        if (value.ValueKind is not JsonValueKind.String and not JsonValueKind.Object)
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' must be a reference id or object.");
        }
    }

    private static void ValidateNumberRange(ContentField field, decimal number)
    {
        if (field.Settings.MinNumber is not null && number < field.Settings.MinNumber)
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' is less than {field.Settings.MinNumber}.");
        }

        if (field.Settings.MaxNumber is not null && number > field.Settings.MaxNumber)
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' is greater than {field.Settings.MaxNumber}.");
        }
    }

    private static void RequireKind(ContentField field, JsonElement value, params JsonValueKind[] allowed)
    {
        if (!allowed.Contains(value.ValueKind))
        {
            throw new DomainRuleViolationException($"Field '{field.ApiName}' has invalid JSON value kind '{value.ValueKind}'.");
        }
    }
}
