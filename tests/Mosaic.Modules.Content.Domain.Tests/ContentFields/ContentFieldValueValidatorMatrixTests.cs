using System.Text.Json;
using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.Tests.ContentFields;

public sealed class ContentFieldValueValidatorMatrixTests
{
    [Theory]
    [MemberData(nameof(ValidationCases))]
    public void Validate_should_match_field_contract(
        string caseName,
        FieldKind kind,
        LocalizationMode localization,
        string json,
        bool isRepeatable,
        int? minLength,
        int? maxLength,
        string? regexPattern,
        decimal? minNumber,
        decimal? maxNumber,
        string[] requiredLocales,
        bool shouldPass)
    {
        var settings = ContentFieldSettings.Create(
            minLength: minLength,
            maxLength: maxLength,
            regexPattern: regexPattern,
            minNumber: minNumber,
            maxNumber: maxNumber,
            requiredLocales: requiredLocales,
            isRepeatable: isRepeatable);
        var field = ContentField.Create(
            $"field{Math.Abs(caseName.GetHashCode(StringComparison.Ordinal))}",
            caseName,
            kind,
            localization,
            isRequired: false,
            settings);

        using var document = JsonDocument.Parse(json);
        var act = () => ContentFieldValueValidator.Validate(field, document.RootElement);

        if (shouldPass)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<DomainRuleViolationException>();
        }
    }

    public static IEnumerable<object?[]> ValidationCases()
    {
        foreach (var value in StringValues().Where(item => item.ShouldPass))
        {
            yield return Case($"string_accept_{value.Name}", FieldKind.String, value.Json);
        }

        foreach (var value in StringValues().Where(item => !item.ShouldPass))
        {
            yield return Case($"string_reject_{value.Name}", FieldKind.String, value.Json, shouldPass: false);
        }

        foreach (var value in TextValues())
        {
            yield return Case($"text_{value.Name}", FieldKind.Text, value.Json, minLength: 3, maxLength: 64, shouldPass: value.ShouldPass);
        }

        foreach (var value in SlugValues())
        {
            yield return Case(
                $"slug_{value.Name}",
                FieldKind.String,
                value.Json,
                regexPattern: "^[a-z0-9-]+$",
                shouldPass: value.ShouldPass);
        }

        foreach (var value in SkuValues())
        {
            yield return Case(
                $"sku_{value.Name}",
                FieldKind.String,
                value.Json,
                minLength: 10,
                maxLength: 32,
                regexPattern: "^[A-Z]{3}-[A-Z]+-[0-9]{3}$",
                shouldPass: value.ShouldPass);
        }

        foreach (var value in BooleanValues())
        {
            yield return Case($"boolean_{value.Name}", FieldKind.Boolean, value.Json, shouldPass: value.ShouldPass);
        }

        foreach (var value in IntegerValues())
        {
            yield return Case(
                $"integer_{value.Name}",
                FieldKind.Integer,
                value.Json,
                minNumber: -10,
                maxNumber: 100,
                shouldPass: value.ShouldPass);
        }

        foreach (var value in DecimalValues())
        {
            yield return Case(
                $"decimal_{value.Name}",
                FieldKind.Decimal,
                value.Json,
                minNumber: 0.01m,
                maxNumber: 9999.99m,
                shouldPass: value.ShouldPass);
        }

        foreach (var value in DateTimeValues())
        {
            yield return Case($"datetime_{value.Name}", FieldKind.DateTime, value.Json, shouldPass: value.ShouldPass);
        }

        foreach (var value in JsonValues())
        {
            yield return Case($"json_{value.Name}", FieldKind.Json, value.Json);
        }

        foreach (var value in ReferenceValues())
        {
            yield return Case($"media_{value.Name}", FieldKind.Media, value.Json, shouldPass: value.ShouldPass);
            yield return Case($"relation_{value.Name}", FieldKind.Relation, value.Json, shouldPass: value.ShouldPass);
        }

        foreach (var value in LocalizedStringValues())
        {
            yield return Case(
                $"localized_string_{value.Name}",
                FieldKind.String,
                value.Json,
                localization: LocalizationMode.Localized,
                minLength: 2,
                requiredLocales: ["ru", "en"],
                shouldPass: value.ShouldPass);
        }

        foreach (var value in LocalizedDecimalValues())
        {
            yield return Case(
                $"localized_decimal_{value.Name}",
                FieldKind.Decimal,
                value.Json,
                localization: LocalizationMode.Localized,
                minNumber: 0,
                maxNumber: 100,
                requiredLocales: ["ru", "en"],
                shouldPass: value.ShouldPass);
        }

        foreach (var value in RepeatableStringValues())
        {
            yield return Case(
                $"repeatable_string_{value.Name}",
                FieldKind.String,
                value.Json,
                isRepeatable: true,
                minLength: 2,
                maxLength: 12,
                shouldPass: value.ShouldPass);
        }

        foreach (var value in RepeatableIntegerValues())
        {
            yield return Case(
                $"repeatable_integer_{value.Name}",
                FieldKind.Integer,
                value.Json,
                isRepeatable: true,
                minNumber: 0,
                maxNumber: 10,
                shouldPass: value.ShouldPass);
        }
    }

    private static object?[] Case(
        string caseName,
        FieldKind kind,
        string json,
        LocalizationMode localization = LocalizationMode.NonLocalized,
        bool isRepeatable = false,
        int? minLength = null,
        int? maxLength = null,
        string? regexPattern = null,
        decimal? minNumber = null,
        decimal? maxNumber = null,
        string[]? requiredLocales = null,
        bool shouldPass = true)
        =>
        [
            caseName,
            kind,
            localization,
            json,
            isRepeatable,
            minLength,
            maxLength,
            regexPattern,
            minNumber,
            maxNumber,
            requiredLocales ?? [],
            shouldPass
        ];

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> StringValues()
    {
        foreach (var text in new[]
                 {
                     "", "a", "simple", "Product", "Продукт", "with space", "with-dash", "with_underscore",
                     "123", "sku-001", "emoji-free", "mixed CASE", "catalog item", "northline", "searchable",
                     "localized fallback", "short", "medium length", "long enough string", "trimmed"
                 })
        {
            yield return (NormalizeName(text), JsonSerializer.Serialize(text), true);
        }

        foreach (var json in new[] { "1", "1.5", "true", "false", "{}", "[]", "{\"ru\":\"Title\"}", "[\"a\"]", "null" })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> TextValues()
    {
        foreach (var text in new[]
                 {
                     "Three", "Description", "A longer paragraph", "Текст товара", "Line with 123", "Office chair",
                     "Compact camera", "Soft light", "Daily backpack", "Work session", "Catalog copy", "Home goods",
                     "Audio notes", "Wearable item", "Minimal object"
                 })
        {
            yield return (NormalizeName(text), JsonSerializer.Serialize(text), true);
        }

        foreach (var json in new[] { "\"\"", "\"a\"", "\"ab\"", "42", "true", "{}", "[]" })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> SlugValues()
    {
        foreach (var text in new[]
                 {
                     "product", "product-1", "northline-daypack", "halo-desk", "studio-flow", "pixel-compact",
                     "frame-work-chair", "motion-band", "sku-001", "category-a", "sale-2026", "new-arrival"
                 })
        {
            yield return (text, JsonSerializer.Serialize(text), true);
        }

        foreach (var text in new[]
                 {
                     "Product", "product name", "product_name", "product.name", "product/name", "продукт",
                     "with space", "UPPER", "mixed-Case", "has@", "has#", "has?"
                 })
        {
            yield return (NormalizeName(text), JsonSerializer.Serialize(text), false);
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> SkuValues()
    {
        foreach (var text in new[]
                 {
                     "NTH-BAG-001", "NTH-LAMP-002", "NTH-HEAD-003", "NTH-CAM-004", "NTH-CHAIR-005",
                     "NTH-WATCH-006", "MSC-PRODUCT-007", "CMS-DEMO-008", "API-CLIENT-009", "WEB-STORE-010",
                     "CAT-HOME-011", "CAT-AUDIO-012", "CAT-PHOTO-013", "CAT-OFFICE-014", "CAT-WEARABLES-015",
                     "PRD-SKU-016", "PRD-ITEM-017", "PRD-CARD-018", "PRD-LIST-019", "PRD-TILE-020",
                     "ORD-LINE-021", "INV-STOCK-022", "INV-PRICE-023", "LOC-TITLE-024", "LOC-DESC-025",
                     "MED-IMAGE-026", "REL-BRAND-027", "REL-CATEGORY-028", "SEA-INDEX-029", "AUD-EVENT-030",
                     "LOG-TRACE-031"
                 })
        {
            yield return (text, JsonSerializer.Serialize(text), true);
        }

        foreach (var text in new[]
                 {
                     "nth-bag-001", "NTH BAG 001", "NTH_BAG_001", "NTH-BAG-1", "NTH-BAG-0001",
                     "NT-BAG-001", "NTH--001", "NTH-BAG", "NTH-001", "NTH-BAG-ABC",
                     "NTH-bag-001", "NTH-Bag-001", "123-BAG-001", "NTH-B4G-001", "NTH-BAG-01A",
                     "NTH/BAG/001", "NTH.BAG.001", "NTH@BAG@001", "NTH-BAG-", "-BAG-001",
                     "NTH- -001", "NTH-BAG 001", "NTH-BAG-00", "NTH-BAG-0000", "NTH-BAG-0O1",
                     "NTH-", "NTH", "", "SKU", "NTH-BAG-001-extra", "NTH-BAG-001 "
                 })
        {
            yield return (NormalizeName(text), JsonSerializer.Serialize(text), false);
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> BooleanValues()
    {
        yield return ("true", "true", true);
        yield return ("false", "false", true);

        foreach (var json in new[] { "\"true\"", "\"false\"", "1", "0", "{}", "[]", "null", "\"yes\"", "\"no\"" })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> IntegerValues()
    {
        foreach (var value in new[] { -10, -1, 0, 1, 2, 5, 10, 42, 99, 100 })
        {
            yield return ($"value_{value}", value.ToString(), true);
        }

        foreach (var json in new[]
                 {
                     "-11", "101", "999", "1.5", "3.14", "\"1\"", "\"42\"", "true", "false", "{}", "[]", "null"
                 })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> DecimalValues()
    {
        foreach (var json in new[] { "0.01", "1", "1.5", "9.99", "10", "42.42", "100", "999.99", "9999.99" })
        {
            yield return (NormalizeName(json), json, true);
        }

        foreach (var json in new[] { "0", "-1", "10000", "10000.01", "\"9.99\"", "true", "{}", "[]", "null" })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> DateTimeValues()
    {
        foreach (var value in new[]
                 {
                     "2026-01-01T00:00:00Z", "2026-05-25T10:15:30Z", "2026-05-25T10:15:30+03:00",
                     "2026-12-31", "2026-02-03T04:05:06.000Z", "2030-01-01T12:00:00Z", "1999-12-31T23:59:59Z",
                     "2026-05-25 10:15:30"
                 })
        {
            yield return (NormalizeName(value), JsonSerializer.Serialize(value), true);
        }

        foreach (var json in new[] { "\"not-a-date\"", "\"2026-99-99\"", "\"tomorrow\"", "123", "true", "{}", "[]", "null" })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json)> JsonValues()
    {
        foreach (var json in new[]
                 {
                     "{}", "[]", "{\"color\":\"red\"}", "{\"nested\":{\"x\":1}}", "[1,2,3]", "\"plain\"",
                     "42", "3.14", "true", "false", "null", "{\"locales\":[\"ru\",\"en\"]}"
                 })
        {
            yield return (NormalizeName(json), json);
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> ReferenceValues()
    {
        foreach (var json in new[]
                 {
                     "\"asset-id\"", "\"550e8400-e29b-41d4-a716-446655440000\"", "{\"id\":\"asset-id\"}",
                     "{\"id\":\"asset-id\",\"type\":\"media\"}", "null"
                 })
        {
            yield return (NormalizeName(json), json, true);
        }

        foreach (var json in new[] { "42", "3.14", "true", "false", "[]", "{\"missingId\":true}" })
        {
            yield return (NormalizeName(json), json, json == "{\"missingId\":true}");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> LocalizedStringValues()
    {
        foreach (var json in new[]
                 {
                     "{\"ru\":\"Товар\",\"en\":\"Product\"}", "{\"ru\":\"Рюкзак\",\"en\":\"Backpack\",\"de\":\"Rucksack\"}",
                     "{\"ru\":\"Лампа\",\"en\":\"Lamp\"}", "{\"ru\":\"Камера\",\"en\":\"Camera\"}", "{\"ru\":\"Кресло\",\"en\":\"Chair\"}",
                     "{\"ru\":\"Часы\",\"en\":\"Watch\"}", "{\"ru\":\"Наушники\",\"en\":\"Headphones\"}", "{\"ru\":\"AA\",\"en\":\"BB\"}"
                 })
        {
            yield return (NormalizeName(json), json, true);
        }

        foreach (var json in new[]
                 {
                     "{\"ru\":\"Товар\"}", "{\"en\":\"Product\"}", "{\"ru\":null,\"en\":\"Product\"}",
                     "{\"ru\":\"A\",\"en\":\"Product\"}", "\"Product\"", "42", "true", "[]", "null"
                 })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> LocalizedDecimalValues()
    {
        foreach (var json in new[]
                 {
                     "{\"ru\":1,\"en\":2}", "{\"ru\":0,\"en\":100}", "{\"ru\":10.5,\"en\":20.75}",
                     "{\"ru\":99.99,\"en\":88.88}", "{\"ru\":42,\"en\":42}"
                 })
        {
            yield return (NormalizeName(json), json, true);
        }

        foreach (var json in new[]
                 {
                     "{\"ru\":-1,\"en\":2}", "{\"ru\":1,\"en\":101}", "{\"ru\":\"1\",\"en\":2}",
                     "{\"ru\":1}", "{\"en\":2}", "\"1\"", "1", "null"
                 })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> RepeatableStringValues()
    {
        foreach (var json in new[] { "[]", "[\"aa\"]", "[\"aa\",\"bb\"]", "[\"tag\",\"sale\",\"new\"]", "[\"northline\",\"office\"]" })
        {
            yield return (NormalizeName(json), json, true);
        }

        foreach (var json in new[] { "\"tag\"", "[\"a\"]", "[\"valid\",\"x\"]", "[1,2]", "[true]", "{}", "null" })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static IEnumerable<(string Name, string Json, bool ShouldPass)> RepeatableIntegerValues()
    {
        foreach (var json in new[] { "[]", "[0]", "[1,2,3]", "[10]", "[0,5,10]" })
        {
            yield return (NormalizeName(json), json, true);
        }

        foreach (var json in new[] { "1", "[11]", "[-1]", "[1.5]", "[\"1\"]", "[true]", "{}", "null" })
        {
            yield return (NormalizeName(json), json, json == "null");
        }
    }

    private static string NormalizeName(string value)
        => value
            .Replace("\"", string.Empty, StringComparison.Ordinal)
            .Replace("{", "object_", StringComparison.Ordinal)
            .Replace("}", string.Empty, StringComparison.Ordinal)
            .Replace("[", "array_", StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Replace(":", "_", StringComparison.Ordinal)
            .Replace(",", "_", StringComparison.Ordinal)
            .Replace(".", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("@", "at", StringComparison.Ordinal)
            .Replace("#", "hash", StringComparison.Ordinal)
            .Replace("?", "question", StringComparison.Ordinal);
}
