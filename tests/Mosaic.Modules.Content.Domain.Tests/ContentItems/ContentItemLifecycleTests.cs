using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Domain.Tests.ContentItems;

public sealed class ContentItemLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    // ── Publish ──────────────────────────────────────────────────────────────

    [Fact]
    public void Publish_should_change_status_to_published()
    {
        var item = CreateDraftItem();
        item.Publish(Now);
        item.Status.Should().Be(ContentItemStatus.Published);
    }

    [Fact]
    public void Publish_should_update_updated_at()
    {
        var item = CreateDraftItem();
        var later = Now.AddMinutes(5);
        item.Publish(later);
        item.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Publish_should_not_throw_when_already_published()
    {
        var item = CreateDraftItem();
        item.Publish(Now);
        var act = () => item.Publish(Now.AddMinutes(1));
        act.Should().NotThrow();
    }

    // ── Unpublish ────────────────────────────────────────────────────────────

    [Fact]
    public void Unpublish_should_change_status_back_to_draft()
    {
        var item = CreateDraftItem();
        item.Publish(Now);
        item.Unpublish(Now.AddMinutes(1));
        item.Status.Should().Be(ContentItemStatus.Draft);
    }

    [Fact]
    public void Unpublish_should_update_updated_at()
    {
        var item = CreateDraftItem();
        item.Publish(Now);
        var later = Now.AddMinutes(10);
        item.Unpublish(later);
        item.UpdatedAt.Should().Be(later);
    }

    // ── Archive ──────────────────────────────────────────────────────────────

    [Fact]
    public void Archive_should_change_status_to_archived()
    {
        var item = CreateDraftItem();
        item.Archive(Now);
        item.Status.Should().Be(ContentItemStatus.Archived);
    }

    [Fact]
    public void Archive_should_work_on_published_item()
    {
        var item = CreateDraftItem();
        item.Publish(Now);
        item.Archive(Now.AddMinutes(1));
        item.Status.Should().Be(ContentItemStatus.Archived);
    }

    [Fact]
    public void Archive_should_not_throw_when_already_archived()
    {
        var item = CreateDraftItem();
        item.Archive(Now);
        var act = () => item.Archive(Now.AddMinutes(1));
        act.Should().NotThrow();
    }

    // ── Archived item cannot be edited ──────────────────────────────────────

    [Fact]
    public void Publish_should_throw_when_item_is_archived()
    {
        var item = CreateDraftItem();
        item.Archive(Now);
        var act = () => item.Publish(Now.AddMinutes(1));
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*Archived*");
    }

    [Fact]
    public void Unpublish_should_throw_when_item_is_archived()
    {
        var item = CreateDraftItem();
        item.Archive(Now);
        var act = () => item.Unpublish(Now.AddMinutes(1));
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*Archived*");
    }

    [Fact]
    public void Update_should_throw_when_item_is_archived()
    {
        var contentType = PublishedProductType();
        var item = ContentItem.Create(contentType, """{"title":{"ru":"Test"}}""", Now);
        item.Archive(Now);
        var act = () => item.Update(contentType, """{"title":{"ru":"Updated"}}""", Now.AddMinutes(1));
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*Archived*");
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_should_change_data_and_updated_at()
    {
        var contentType = PublishedProductType();
        var item = ContentItem.Create(contentType, """{"title":{"ru":"Old"}}""", Now);
        var later = Now.AddMinutes(5);

        item.Update(contentType, """{"title":{"ru":"New"}}""", later);

        item.Data.Should().Contain("New");
        item.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Update_should_not_change_created_at()
    {
        var contentType = PublishedProductType();
        var item = ContentItem.Create(contentType, """{"title":{"ru":"Old"}}""", Now);

        item.Update(contentType, """{"title":{"ru":"New"}}""", Now.AddMinutes(5));

        item.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Update_should_validate_new_data()
    {
        var contentType = PublishedProductType();
        var item = ContentItem.Create(contentType, """{"title":{"ru":"Old"}}""", Now);

        var act = () => item.Update(contentType, "{}", Now.AddMinutes(1));
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*Required field*");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ContentItem CreateDraftItem()
    {
        var contentType = PublishedProductType();
        return ContentItem.Create(contentType, """{"title":{"ru":"Test"}}""", Now);
    }

    private static ContentType PublishedProductType()
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
