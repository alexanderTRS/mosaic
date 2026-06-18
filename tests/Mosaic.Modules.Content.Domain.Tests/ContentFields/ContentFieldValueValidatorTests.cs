using System.Text.Json;
using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.Tests.ContentFields;

public sealed class ContentFieldValueValidatorTests
{
    // ── String ──────────────────────────────────────────────────────────────

    [Fact]
    public void String_field_should_accept_valid_string()
    {
        var field = StringField("title");
        Validate(field, "\"hello\"");
    }

    [Fact]
    public void String_field_should_reject_number_value()
    {
        var field = StringField("title");
        var act = () => Validate(field, "42");
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void String_field_should_reject_boolean_value()
    {
        var field = StringField("title");
        var act = () => Validate(field, "true");
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void String_field_should_reject_value_shorter_than_min_length()
    {
        var field = ContentField.Create("slug", "Slug", FieldKind.String, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(minLength: 5));
        var act = () => Validate(field, "\"ab\"");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*shorter*");
    }

    [Fact]
    public void String_field_should_reject_value_longer_than_max_length()
    {
        var field = ContentField.Create("slug", "Slug", FieldKind.String, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(maxLength: 3));
        var act = () => Validate(field, "\"toolong\"");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*longer*");
    }

    [Fact]
    public void String_field_should_accept_value_at_exact_min_length()
    {
        var field = ContentField.Create("slug", "Slug", FieldKind.String, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(minLength: 3));
        var act = () => Validate(field, "\"abc\"");
        act.Should().NotThrow();
    }

    [Fact]
    public void String_field_should_reject_value_not_matching_regex()
    {
        var field = ContentField.Create("slug", "Slug", FieldKind.String, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(regexPattern: "^[a-z0-9-]+$"));
        var act = () => Validate(field, "\"Hello World\"");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*pattern*");
    }

    [Fact]
    public void String_field_should_accept_value_matching_regex()
    {
        var field = ContentField.Create("slug", "Slug", FieldKind.String, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(regexPattern: "^[a-z0-9-]+$"));
        var act = () => Validate(field, "\"hello-world\"");
        act.Should().NotThrow();
    }

    // ── Boolean ─────────────────────────────────────────────────────────────

    [Fact]
    public void Boolean_field_should_accept_true()
    {
        var field = BoolField("inStock");
        var act = () => Validate(field, "true");
        act.Should().NotThrow();
    }

    [Fact]
    public void Boolean_field_should_accept_false()
    {
        var field = BoolField("inStock");
        var act = () => Validate(field, "false");
        act.Should().NotThrow();
    }

    [Fact]
    public void Boolean_field_should_reject_string_value()
    {
        var field = BoolField("inStock");
        var act = () => Validate(field, "\"true\"");
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Boolean_field_should_reject_number_value()
    {
        var field = BoolField("inStock");
        var act = () => Validate(field, "1");
        act.Should().Throw<DomainRuleViolationException>();
    }

    // ── Integer ─────────────────────────────────────────────────────────────

    [Fact]
    public void Integer_field_should_accept_integer_value()
    {
        var field = IntField("quantity");
        var act = () => Validate(field, "42");
        act.Should().NotThrow();
    }

    [Fact]
    public void Integer_field_should_reject_decimal_value()
    {
        var field = IntField("quantity");
        var act = () => Validate(field, "3.14");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*integer*");
    }

    [Fact]
    public void Integer_field_should_reject_string_value()
    {
        var field = IntField("quantity");
        var act = () => Validate(field, "\"42\"");
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Integer_field_should_reject_value_below_min()
    {
        var field = ContentField.Create("qty", "Qty", FieldKind.Integer, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(minNumber: 1));
        var act = () => Validate(field, "0");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*less than*");
    }

    [Fact]
    public void Integer_field_should_reject_value_above_max()
    {
        var field = ContentField.Create("qty", "Qty", FieldKind.Integer, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(maxNumber: 99));
        var act = () => Validate(field, "100");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*greater than*");
    }

    // ── Decimal ─────────────────────────────────────────────────────────────

    [Fact]
    public void Decimal_field_should_accept_decimal_value()
    {
        var field = DecimalField("price");
        var act = () => Validate(field, "9.99");
        act.Should().NotThrow();
    }

    [Fact]
    public void Decimal_field_should_accept_integer_as_decimal()
    {
        var field = DecimalField("price");
        var act = () => Validate(field, "10");
        act.Should().NotThrow();
    }

    [Fact]
    public void Decimal_field_should_reject_string_value()
    {
        var field = DecimalField("price");
        var act = () => Validate(field, "\"9.99\"");
        act.Should().Throw<DomainRuleViolationException>();
    }

    // ── DateTime ────────────────────────────────────────────────────────────

    [Fact]
    public void DateTime_field_should_accept_iso_date_string()
    {
        var field = DateTimeField("publishedAt");
        var act = () => Validate(field, "\"2026-01-15T10:00:00Z\"");
        act.Should().NotThrow();
    }

    [Fact]
    public void DateTime_field_should_reject_non_date_string()
    {
        var field = DateTimeField("publishedAt");
        var act = () => Validate(field, "\"not-a-date\"");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*ISO date-time*");
    }

    [Fact]
    public void DateTime_field_should_reject_number_value()
    {
        var field = DateTimeField("publishedAt");
        var act = () => Validate(field, "1234567890");
        act.Should().Throw<DomainRuleViolationException>();
    }

    // ── JSON ────────────────────────────────────────────────────────────────

    [Fact]
    public void Json_field_should_accept_any_json_value()
    {
        var field = ContentField.Create("meta", "Meta", FieldKind.Json, LocalizationMode.NonLocalized, false);
        Validate(field, "{\"key\":\"value\"}");
        Validate(field, "[1,2,3]");
        Validate(field, "\"string\"");
        Validate(field, "42");
        Validate(field, "true");
    }

    // ── Media / Relation ────────────────────────────────────────────────────

    [Fact]
    public void Media_field_should_accept_string_id()
    {
        var field = ContentField.Create("image", "Image", FieldKind.Media, LocalizationMode.NonLocalized, false);
        var act = () => Validate(field, "\"some-uuid\"");
        act.Should().NotThrow();
    }

    [Fact]
    public void Media_field_should_accept_object_reference()
    {
        var field = ContentField.Create("image", "Image", FieldKind.Media, LocalizationMode.NonLocalized, false);
        var act = () => Validate(field, "{\"id\":\"some-uuid\"}");
        act.Should().NotThrow();
    }

    [Fact]
    public void Media_field_should_reject_number_value()
    {
        var field = ContentField.Create("image", "Image", FieldKind.Media, LocalizationMode.NonLocalized, false);
        var act = () => Validate(field, "42");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*reference*");
    }

    [Fact]
    public void Relation_field_should_accept_string_id()
    {
        var field = ContentField.Create("category", "Category", FieldKind.Relation, LocalizationMode.NonLocalized, false);
        var act = () => Validate(field, "\"cat-id\"");
        act.Should().NotThrow();
    }

    // ── Localized ───────────────────────────────────────────────────────────

    [Fact]
    public void Localized_field_should_accept_object_with_locales()
    {
        var field = ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.Localized, false);
        var act = () => Validate(field, "{\"en\":\"Hello\",\"ru\":\"Привет\"}");
        act.Should().NotThrow();
    }

    [Fact]
    public void Localized_field_should_reject_non_object_value()
    {
        var field = ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.Localized, false);
        var act = () => Validate(field, "\"Hello\"");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*JSON object*");
    }

    [Fact]
    public void Localized_field_should_reject_missing_required_locale()
    {
        var field = ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.Localized, false,
            ContentFieldSettings.Create(requiredLocales: ["en", "ru"]));
        var act = () => Validate(field, "{\"en\":\"Hello\"}");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*locale*ru*");
    }

    [Fact]
    public void Localized_field_should_validate_each_locale_value()
    {
        var field = ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.Localized, false,
            ContentFieldSettings.Create(minLength: 3));
        var act = () => Validate(field, "{\"en\":\"Hi\"}");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*shorter*");
    }

    // ── Repeatable ──────────────────────────────────────────────────────────

    [Fact]
    public void Repeatable_field_should_accept_array()
    {
        var field = ContentField.Create("tags", "Tags", FieldKind.String, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(isRepeatable: true));
        var act = () => Validate(field, "[\"a\",\"b\"]");
        act.Should().NotThrow();
    }

    [Fact]
    public void Repeatable_field_should_reject_non_array()
    {
        var field = ContentField.Create("tags", "Tags", FieldKind.String, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(isRepeatable: true));
        var act = () => Validate(field, "\"single\"");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*array*");
    }

    [Fact]
    public void Repeatable_field_should_validate_each_item()
    {
        var field = ContentField.Create("tags", "Tags", FieldKind.String, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(isRepeatable: true, minLength: 3));
        var act = () => Validate(field, "[\"ok\",\"x\"]");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*shorter*");
    }

    // ── Null passthrough ────────────────────────────────────────────────────

    [Fact]
    public void Null_value_should_pass_validation_for_any_field()
    {
        var field = StringField("title");
        var act = () => Validate(field, "null");
        act.Should().NotThrow();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static void Validate(ContentField field, string json)
    {
        using var doc = JsonDocument.Parse(json);
        ContentFieldValueValidator.Validate(field, doc.RootElement);
    }

    private static ContentField StringField(string name) =>
        ContentField.Create(name, name, FieldKind.String, LocalizationMode.NonLocalized, false);

    private static ContentField BoolField(string name) =>
        ContentField.Create(name, name, FieldKind.Boolean, LocalizationMode.NonLocalized, false);

    private static ContentField IntField(string name) =>
        ContentField.Create(name, name, FieldKind.Integer, LocalizationMode.NonLocalized, false);

    private static ContentField DecimalField(string name) =>
        ContentField.Create(name, name, FieldKind.Decimal, LocalizationMode.NonLocalized, false);

    private static ContentField DateTimeField(string name) =>
        ContentField.Create(name, name, FieldKind.DateTime, LocalizationMode.NonLocalized, false);
}
