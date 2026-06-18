using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.Tests.ContentTypes;

public sealed class ContentFieldSettingsTests
{
    [Fact]
    public void Create_with_no_args_should_return_empty_settings()
    {
        var settings = ContentFieldSettings.Create();

        settings.MinLength.Should().BeNull();
        settings.MaxLength.Should().BeNull();
        settings.RegexPattern.Should().BeNull();
        settings.MinNumber.Should().BeNull();
        settings.MaxNumber.Should().BeNull();
        settings.RequiredLocales.Should().BeEmpty();
        settings.IsUnique.Should().BeFalse();
        settings.IsRepeatable.Should().BeFalse();
        settings.DefaultValue.Should().BeNull();
        settings.RelationTargetContentTypeApiName.Should().BeNull();
    }

    [Fact]
    public void Create_should_reject_negative_min_length()
    {
        var act = () => ContentFieldSettings.Create(minLength: -1);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_should_reject_negative_max_length()
    {
        var act = () => ContentFieldSettings.Create(maxLength: -1);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_should_reject_min_length_greater_than_max_length()
    {
        var act = () => ContentFieldSettings.Create(minLength: 10, maxLength: 5);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*greater than maximum*");
    }

    [Fact]
    public void Create_should_accept_equal_min_and_max_length()
    {
        var act = () => ContentFieldSettings.Create(minLength: 5, maxLength: 5);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_reject_min_number_greater_than_max_number()
    {
        var act = () => ContentFieldSettings.Create(minNumber: 100, maxNumber: 10);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*greater than maximum*");
    }

    [Fact]
    public void Create_should_accept_equal_min_and_max_number()
    {
        var act = () => ContentFieldSettings.Create(minNumber: 5, maxNumber: 5);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_reject_invalid_regex_pattern()
    {
        var act = () => ContentFieldSettings.Create(regexPattern: "[invalid(");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_should_accept_valid_regex_pattern()
    {
        var settings = ContentFieldSettings.Create(regexPattern: "^[a-z0-9-]+$");
        settings.RegexPattern.Should().Be("^[a-z0-9-]+$");
    }

    [Fact]
    public void Create_should_reject_invalid_default_value_json()
    {
        var act = () => ContentFieldSettings.Create(defaultValue: "not-json{");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_should_accept_valid_default_value_json()
    {
        var settings = ContentFieldSettings.Create(defaultValue: "true");
        settings.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void Create_should_normalize_required_locales_to_lowercase_trimmed()
    {
        var settings = ContentFieldSettings.Create(requiredLocales: ["  RU  ", "EN", "ru"]);
        settings.RequiredLocales.Should().BeEquivalentTo(["ru", "en"]);
    }

    [Fact]
    public void Create_should_reject_invalid_relation_target_api_name()
    {
        var act = () => ContentFieldSettings.Create(relationTargetContentTypeApiName: "Invalid-Name");
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_accept_valid_relation_target_api_name()
    {
        var settings = ContentFieldSettings.Create(relationTargetContentTypeApiName: "category");
        settings.RelationTargetContentTypeApiName.Should().Be("category");
    }

    [Fact]
    public void Create_should_treat_whitespace_only_regex_as_null()
    {
        var settings = ContentFieldSettings.Create(regexPattern: "   ");
        settings.RegexPattern.Should().BeNull();
    }

    [Fact]
    public void Create_should_treat_whitespace_only_default_value_as_null()
    {
        var settings = ContentFieldSettings.Create(defaultValue: "   ");
        settings.DefaultValue.Should().BeNull();
    }
}
