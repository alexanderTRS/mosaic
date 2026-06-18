using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Domain.Tests.ContentItems;

public sealed class ContentItemTests
{
    [Fact]
    public void Create_should_create_draft_item_for_published_content_type()
    {
        var contentType = PublishedProductType();
        var now = new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero);

        var item = ContentItem.Create(contentType, """{"title":{"ru":"iPhone 15"}}""", now);

        item.ContentTypeId.Should().Be(contentType.Id);
        item.Status.Should().Be(ContentItemStatus.Draft);
        item.CreatedAt.Should().Be(now);
        item.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_should_reject_unpublished_content_type()
    {
        var contentType = ContentType.Create("product", "Product");

        var act = () => ContentItem.Create(contentType, """{"title":{"ru":"iPhone 15"}}""", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_missing_required_field()
    {
        var contentType = PublishedProductType();

        var act = () => ContentItem.Create(contentType, "{}", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_non_object_json()
    {
        var contentType = PublishedProductType();

        var act = () => ContentItem.Create(contentType, "[]", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_string_shorter_than_min_length()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: true,
            ContentFieldSettings.Create(minLength: 3)));
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        var act = () => ContentItem.Create(contentType, """{"title":"TV"}""", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_integer_outside_number_range()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "quantity",
            "Quantity",
            FieldKind.Integer,
            LocalizationMode.NonLocalized,
            isRequired: true,
            ContentFieldSettings.Create(minNumber: 1, maxNumber: 99)));
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        var act = () => ContentItem.Create(contentType, """{"quantity":100}""", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_missing_required_locale()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.Localized,
            isRequired: true,
            ContentFieldSettings.Create(requiredLocales: ["ru", "en"])));
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        var act = () => ContentItem.Create(contentType, """{"title":{"ru":"Phone"}}""", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_validate_repeatable_field_items()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "tags",
            "Tags",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: false,
            ContentFieldSettings.Create(isRepeatable: true, minLength: 2)));
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        var act = () => ContentItem.Create(contentType, """{"tags":["ok","x"]}""", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_apply_default_value_when_field_is_missing()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "isActive",
            "Is Active",
            FieldKind.Boolean,
            LocalizationMode.NonLocalized,
            isRequired: false,
            ContentFieldSettings.Create(defaultValue: "true")));
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        var item = ContentItem.Create(contentType, "{}", DateTimeOffset.UtcNow);

        item.Data.Should().Contain("\"isActive\":true");
    }

    private static ContentType PublishedProductType()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.Localized,
            isRequired: true));
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));

        return contentType;
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
