using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.Tests.ContentFields;

public sealed class ContentFieldTests
{
    [Fact]
    public void Create_should_set_all_properties()
    {
        var field = ContentField.Create(
            "title", "Title", FieldKind.String, LocalizationMode.Localized, isRequired: true);

        field.ApiName.Value.Should().Be("title");
        field.DisplayName.Should().Be("Title");
        field.Kind.Should().Be(FieldKind.String);
        field.Localization.Should().Be(LocalizationMode.Localized);
        field.IsRequired.Should().BeTrue();
        field.IsDeprecated.Should().BeFalse();
        field.IsLocalized.Should().BeTrue();
    }

    [Fact]
    public void Create_should_reject_empty_display_name()
    {
        var act = () => ContentField.Create("title", "   ", FieldKind.String, LocalizationMode.NonLocalized, false);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_trim_display_name()
    {
        var field = ContentField.Create("title", "  Title  ", FieldKind.String, LocalizationMode.NonLocalized, false);
        field.DisplayName.Should().Be("Title");
    }

    [Fact]
    public void Create_should_use_empty_settings_when_null_passed()
    {
        var field = ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.NonLocalized, false, null);
        field.Settings.Should().Be(ContentFieldSettings.Empty);
    }

    [Fact]
    public void IsLocalized_should_be_false_for_non_localized_field()
    {
        var field = ContentField.Create("slug", "Slug", FieldKind.String, LocalizationMode.NonLocalized, false);
        field.IsLocalized.Should().BeFalse();
    }

    [Fact]
    public void Deprecate_should_mark_field_as_deprecated()
    {
        var field = ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.NonLocalized, false);
        field.Deprecate();
        field.IsDeprecated.Should().BeTrue();
    }

    [Fact]
    public void Create_should_reject_string_rules_on_boolean_field()
    {
        var act = () => ContentField.Create(
            "flag", "Flag", FieldKind.Boolean, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(minLength: 1));
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_string_rules_on_integer_field()
    {
        var act = () => ContentField.Create(
            "count", "Count", FieldKind.Integer, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(maxLength: 10));
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_number_rules_on_text_field()
    {
        var act = () => ContentField.Create(
            "body", "Body", FieldKind.Text, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(minNumber: 1));
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_number_rules_on_boolean_field()
    {
        var act = () => ContentField.Create(
            "flag", "Flag", FieldKind.Boolean, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(maxNumber: 100));
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_relation_target_on_non_relation_field()
    {
        var act = () => ContentField.Create(
            "category", "Category", FieldKind.String, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(relationTargetContentTypeApiName: "category"));
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_accept_relation_target_on_relation_field()
    {
        var act = () => ContentField.Create(
            "category", "Category", FieldKind.Relation, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(relationTargetContentTypeApiName: "category"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_accept_string_rules_on_text_field()
    {
        var act = () => ContentField.Create(
            "body", "Body", FieldKind.Text, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(minLength: 10, maxLength: 5000));
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_accept_number_rules_on_decimal_field()
    {
        var act = () => ContentField.Create(
            "price", "Price", FieldKind.Decimal, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(minNumber: 0, maxNumber: 999999));
        act.Should().NotThrow();
    }
}
