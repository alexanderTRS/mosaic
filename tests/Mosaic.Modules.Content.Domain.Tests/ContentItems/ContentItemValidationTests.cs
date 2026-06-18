using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Domain.Tests.ContentItems;

public sealed class ContentItemValidationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_should_reject_json_array_as_data()
    {
        var ct = PublishedType();
        var act = () => ContentItem.Create(ct, "[]", Now);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*JSON object*");
    }

    [Fact]
    public void Create_should_reject_json_string_as_data()
    {
        var ct = PublishedType();
        var act = () => ContentItem.Create(ct, "\"hello\"", Now);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_invalid_json()
    {
        var ct = PublishedType();
        var act = () => ContentItem.Create(ct, "not-json", Now);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_should_accept_empty_object_when_no_required_fields()
    {
        var ct = ContentType.Create("tag", "Tag");
        ct.AddField(ContentField.Create("label", "Label", FieldKind.String, LocalizationMode.NonLocalized, isRequired: false));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, "{}", Now);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_reject_boolean_field_with_string_value()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("inStock", "In Stock", FieldKind.Boolean, LocalizationMode.NonLocalized, isRequired: true));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, """{"inStock":"yes"}""", Now);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_accept_boolean_field_with_true_value()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("inStock", "In Stock", FieldKind.Boolean, LocalizationMode.NonLocalized, isRequired: true));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, """{"inStock":true}""", Now);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_reject_datetime_field_with_invalid_string()
    {
        var ct = ContentType.Create("event", "Event");
        ct.AddField(ContentField.Create("startsAt", "Starts At", FieldKind.DateTime, LocalizationMode.NonLocalized, isRequired: true));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, """{"startsAt":"not-a-date"}""", Now);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*ISO date-time*");
    }

    [Fact]
    public void Create_should_accept_datetime_field_with_valid_iso_string()
    {
        var ct = ContentType.Create("event", "Event");
        ct.AddField(ContentField.Create("startsAt", "Starts At", FieldKind.DateTime, LocalizationMode.NonLocalized, isRequired: true));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, """{"startsAt":"2026-06-01T09:00:00Z"}""", Now);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_accept_json_field_with_any_value()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("attributes", "Attributes", FieldKind.Json, LocalizationMode.NonLocalized, isRequired: true));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, """{"attributes":{"color":"red","size":42}}""", Now);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_apply_multiple_default_values()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("inStock", "In Stock", FieldKind.Boolean, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(defaultValue: "true")));
        ct.AddField(ContentField.Create("rating", "Rating", FieldKind.Decimal, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(defaultValue: "0.0")));
        ct.Publish(new FixedClock(Now));

        var item = ContentItem.Create(ct, "{}", Now);

        item.Data.Should().Contain("\"inStock\":true");
        item.Data.Should().Contain("\"rating\":0.0");
    }

    [Fact]
    public void Create_should_not_override_existing_value_with_default()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("inStock", "In Stock", FieldKind.Boolean, LocalizationMode.NonLocalized, false,
            ContentFieldSettings.Create(defaultValue: "true")));
        ct.Publish(new FixedClock(Now));

        var item = ContentItem.Create(ct, """{"inStock":false}""", Now);

        item.Data.Should().Contain("\"inStock\":false");
    }

    [Fact]
    public void Create_should_reject_string_field_with_value_exceeding_max_length()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("sku", "SKU", FieldKind.String, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(maxLength: 5)));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, """{"sku":"TOOLONGVALUE"}""", Now);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*longer*");
    }

    [Fact]
    public void Create_should_reject_decimal_field_below_min()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("price", "Price", FieldKind.Decimal, LocalizationMode.NonLocalized, true,
            ContentFieldSettings.Create(minNumber: 0.01m)));
        ct.Publish(new FixedClock(Now));

        var act = () => ContentItem.Create(ct, """{"price":0}""", Now);
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*less than*");
    }

    [Fact]
    public void Create_should_set_content_type_id()
    {
        var ct = PublishedType();
        var item = ContentItem.Create(ct, """{"title":{"ru":"Test"}}""", Now);
        item.ContentTypeId.Should().Be(ct.Id);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ContentType PublishedType()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.Localized, isRequired: true));
        ct.Publish(new FixedClock(Now));
        return ct;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
