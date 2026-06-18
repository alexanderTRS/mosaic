using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Domain.Tests.ContentTypes;

public sealed class ContentTypeEvolutionTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Publish_is_idempotent_and_does_not_change_published_at_on_second_call()
    {
        var ct = ContentType.Create("product", "Product");
        ct.Publish(new FixedClock(Now));
        var firstPublishedAt = ct.PublishedAt;

        ct.Publish(new FixedClock(Now.AddHours(1)));

        ct.PublishedAt.Should().Be(firstPublishedAt);
    }

    [Fact]
    public void Schema_version_starts_at_one()
    {
        var ct = ContentType.Create("product", "Product");
        ct.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void Adding_multiple_fields_increments_version_each_time()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.NonLocalized, false));
        ct.AddField(ContentField.Create("slug", "Slug", FieldKind.String, LocalizationMode.NonLocalized, false));
        ct.SchemaVersion.Should().Be(3);
    }

    [Fact]
    public void DeprecateField_on_already_deprecated_field_does_not_increment_version()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.NonLocalized, false));
        ct.DeprecateField("title");
        var versionAfterFirstDeprecation = ct.SchemaVersion;

        ct.DeprecateField("title");

        ct.SchemaVersion.Should().Be(versionAfterFirstDeprecation);
    }

    [Fact]
    public void DeprecateField_should_throw_when_field_does_not_exist()
    {
        var ct = ContentType.Create("product", "Product");
        var act = () => ct.DeprecateField("nonexistent");
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*does not exist*");
    }

    [Fact]
    public void AddField_should_reject_contentType_reserved_name()
    {
        var ct = ContentType.Create("product", "Product");
        var act = () => ct.AddField(ContentField.Create(
            "contentType", "Content Type", FieldKind.String, LocalizationMode.NonLocalized, false));
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*reserved*");
    }

    [Fact]
    public void Published_content_type_can_have_multiple_optional_fields_added()
    {
        var ct = ContentType.Create("product", "Product");
        ct.Publish(new FixedClock(Now));

        ct.AddField(ContentField.Create("tag", "Tag", FieldKind.String, LocalizationMode.NonLocalized, false));
        ct.AddField(ContentField.Create("note", "Note", FieldKind.Text, LocalizationMode.NonLocalized, false));

        ct.Fields.Should().HaveCount(2);
    }

    [Fact]
    public void Create_should_reject_empty_display_name()
    {
        var act = () => ContentType.Create("product", "   ");
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Fields_collection_is_empty_on_creation()
    {
        var ct = ContentType.Create("product", "Product");
        ct.Fields.Should().BeEmpty();
    }

    [Fact]
    public void Status_is_draft_on_creation()
    {
        var ct = ContentType.Create("product", "Product");
        ct.Status.Should().Be(ContentTypeStatus.Draft);
        ct.PublishedAt.Should().BeNull();
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
