using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Time;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.Tests.ContentTypes;

public sealed class ContentTypeTests
{
    [Fact]
    public void Create_should_normalize_and_expose_graphql_type_name()
    {
        var contentType = ContentType.Create("product", "Product");

        contentType.ApiName.Value.Should().Be("product");
        contentType.ApiName.GraphQlTypeName.Should().Be("Product");
        contentType.DisplayName.Should().Be("Product");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Product")]
    [InlineData("product-name")]
    [InlineData("1product")]
    public void Create_should_reject_invalid_api_name(string apiName)
    {
        var act = () => ContentType.Create(apiName, "Product");

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void AddField_should_keep_localization_rule_on_field()
    {
        var contentType = ContentType.Create("product", "Product");
        var title = ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.Localized,
            isRequired: true);

        contentType.AddField(title);

        contentType.Fields.Should().ContainSingle();
        contentType.Fields.Single().IsLocalized.Should().BeTrue();
    }

    [Fact]
    public void AddField_should_reject_duplicate_field_api_name()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.Localized,
            isRequired: true));

        var act = () => contentType.AddField(ContentField.Create(
            "title",
            "Another title",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: false));

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("createdAt")]
    [InlineData("updatedAt")]
    [InlineData("publishedAt")]
    public void AddField_should_reject_reserved_field_names(string fieldName)
    {
        var contentType = ContentType.Create("product", "Product");

        var act = () => contentType.AddField(ContentField.Create(
            fieldName,
            "Reserved",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: false));

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Publish_should_mark_content_type_as_published()
    {
        var contentType = ContentType.Create("product", "Product");
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero));

        contentType.Publish(clock);

        contentType.Status.Should().Be(ContentTypeStatus.Published);
        contentType.PublishedAt.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void AddField_should_increment_schema_version()
    {
        var contentType = ContentType.Create("product", "Product");
        var originalVersion = contentType.SchemaVersion;

        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: false));

        contentType.SchemaVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public void DeprecateField_should_mark_field_and_increment_schema_version()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: false));
        var versionAfterAdd = contentType.SchemaVersion;

        contentType.DeprecateField("title");

        contentType.Fields.Single().IsDeprecated.Should().BeTrue();
        contentType.SchemaVersion.Should().Be(versionAfterAdd + 1);
    }

    [Fact]
    public void AddField_should_allow_optional_fields_after_content_type_is_published()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.Localized,
            isRequired: false));

        contentType.Fields.Should().ContainSingle(field => field.ApiName.Value == "title");
    }

    [Fact]
    public void AddField_should_reject_required_fields_after_content_type_is_published()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        var act = () => contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.Localized,
            isRequired: true));

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_field_should_reject_number_rules_for_string_field()
    {
        var act = () => ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: false,
            ContentFieldSettings.Create(minNumber: 1));

        act.Should().Throw<DomainRuleViolationException>();
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
