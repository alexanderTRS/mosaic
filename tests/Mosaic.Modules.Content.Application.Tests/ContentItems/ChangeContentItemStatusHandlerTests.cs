using FluentAssertions;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Application.Tests.ContentItems;

public sealed class ChangeContentItemStatusHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    // ── Publish ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Publish_should_change_status_to_published_and_save()
    {
        var (contentType, item, itemRepo, uow) = Setup();
        var handler = BuildPublishHandler(new FakeContentTypeRepository(contentType), itemRepo, uow);

        var result = await handler.Handle(new ChangeContentItemStatusCommand(item.Id), CancellationToken.None);

        result.Status.Should().Be(ContentItemStatus.Published);
        uow.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Publish_should_throw_when_item_not_found()
    {
        var handler = BuildPublishHandler(new FakeContentTypeRepository(), new FakeContentItemRepository(), new FakeContentUnitOfWork());
        var act = async () => await handler.Handle(new ChangeContentItemStatusCommand(ContentItemId.New()), CancellationToken.None);
        await act.Should().ThrowAsync<ContentItemNotFoundException>();
    }

    [Fact]
    public async Task Publish_should_create_version_snapshot()
    {
        var (contentType, item, itemRepo, uow) = Setup();
        var handler = BuildPublishHandler(new FakeContentTypeRepository(contentType), itemRepo, uow);

        await handler.Handle(new ChangeContentItemStatusCommand(item.Id), CancellationToken.None);

        itemRepo.VersionedItem.Should().NotBeNull();
    }

    // ── Unpublish ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unpublish_should_change_status_to_draft_and_save()
    {
        var (contentType, item, itemRepo, uow) = Setup();
        item.Publish(Now);
        var handler = BuildUnpublishHandler(new FakeContentTypeRepository(contentType), itemRepo, uow);

        var result = await handler.Handle(new ChangeContentItemStatusCommand(item.Id), CancellationToken.None);

        result.Status.Should().Be(ContentItemStatus.Draft);
        uow.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Unpublish_should_throw_when_item_not_found()
    {
        var handler = BuildUnpublishHandler(new FakeContentTypeRepository(), new FakeContentItemRepository(), new FakeContentUnitOfWork());
        var act = async () => await handler.Handle(new ChangeContentItemStatusCommand(ContentItemId.New()), CancellationToken.None);
        await act.Should().ThrowAsync<ContentItemNotFoundException>();
    }

    // ── Archive ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Archive_should_change_status_to_archived_and_save()
    {
        var (contentType, item, itemRepo, uow) = Setup();
        var handler = BuildArchiveHandler(new FakeContentTypeRepository(contentType), itemRepo, uow);

        var result = await handler.Handle(new ChangeContentItemStatusCommand(item.Id), CancellationToken.None);

        result.Status.Should().Be(ContentItemStatus.Archived);
        uow.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Archive_should_throw_when_item_not_found()
    {
        var handler = BuildArchiveHandler(new FakeContentTypeRepository(), new FakeContentItemRepository(), new FakeContentUnitOfWork());
        var act = async () => await handler.Handle(new ChangeContentItemStatusCommand(ContentItemId.New()), CancellationToken.None);
        await act.Should().ThrowAsync<ContentItemNotFoundException>();
    }

    [Fact]
    public async Task Archive_should_create_version_snapshot()
    {
        var (contentType, item, itemRepo, uow) = Setup();
        var handler = BuildArchiveHandler(new FakeContentTypeRepository(contentType), itemRepo, uow);

        await handler.Handle(new ChangeContentItemStatusCommand(item.Id), CancellationToken.None);

        itemRepo.VersionedItem.Should().NotBeNull();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (ContentType, ContentItem, FakeContentItemRepository, FakeContentUnitOfWork) Setup()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.Localized, true));
        ct.Publish(new FixedClock(Now));
        var item = ContentItem.Create(ct, """{"title":{"ru":"Test"}}""", Now);
        return (ct, item, new FakeContentItemRepository(item), new FakeContentUnitOfWork());
    }

    private static PublishContentItemHandler BuildPublishHandler(
        FakeContentTypeRepository typeRepo, FakeContentItemRepository itemRepo, FakeContentUnitOfWork uow)
    {
        var mutationService = new ContentItemMutationService(typeRepo, itemRepo, new AllowAllContentAccessService(), new FixedClock(Now.AddMinutes(1)));
        return new PublishContentItemHandler(itemRepo, uow, mutationService, new NullAuditLog());
    }

    private static UnpublishContentItemHandler BuildUnpublishHandler(
        FakeContentTypeRepository typeRepo, FakeContentItemRepository itemRepo, FakeContentUnitOfWork uow)
    {
        var mutationService = new ContentItemMutationService(typeRepo, itemRepo, new AllowAllContentAccessService(), new FixedClock(Now.AddMinutes(1)));
        return new UnpublishContentItemHandler(itemRepo, uow, mutationService, new NullAuditLog());
    }

    private static ArchiveContentItemHandler BuildArchiveHandler(
        FakeContentTypeRepository typeRepo, FakeContentItemRepository itemRepo, FakeContentUnitOfWork uow)
    {
        var mutationService = new ContentItemMutationService(typeRepo, itemRepo, new AllowAllContentAccessService(), new FixedClock(Now.AddMinutes(1)));
        return new ArchiveContentItemHandler(itemRepo, uow, mutationService, new NullAuditLog());
    }

    private sealed class FakeContentTypeRepository : IContentTypeRepository
    {
        private readonly ContentType? ct;
        public FakeContentTypeRepository(ContentType? ct = null) => this.ct = ct;
        public Task<ContentType?> GetById(ContentTypeId id, CancellationToken c) => Task.FromResult(ct?.Id == id ? ct : null);
        public Task<ContentType?> GetByApiName(string name, CancellationToken c) => Task.FromResult(ct?.ApiName.Value == name ? ct : null);
        public Task<IReadOnlyCollection<ContentType>> List(CancellationToken c) => Task.FromResult<IReadOnlyCollection<ContentType>>(ct is null ? [] : [ct]);
        public Task Add(ContentType t, CancellationToken c) => Task.CompletedTask;
        public Task Update(ContentType t, CancellationToken c) => Task.CompletedTask;
    }

    private sealed class FakeContentItemRepository : IContentItemRepository
    {
        private readonly ContentItem? item;
        public FakeContentItemRepository(ContentItem? item = null) => this.item = item;
        public ContentItem? VersionedItem { get; private set; }
        public Task<ContentItem?> GetDomainById(ContentItemId id, CancellationToken c) => Task.FromResult(item?.Id == id ? item : null);
        public Task<ContentItemDetails?> GetById(ContentItemId id, CancellationToken c) => Task.FromResult<ContentItemDetails?>(null);
        public Task<IReadOnlyCollection<ContentItemDetails>> List(string? type, CancellationToken c) => Task.FromResult<IReadOnlyCollection<ContentItemDetails>>([]);
        public Task<ContentItemsPage> Page(ListContentItemsQuery q, CancellationToken c) => Task.FromResult(new ContentItemsPage([], 0, q.Skip, q.Take));
        public Task<IReadOnlyCollection<ContentItemVersionDetails>> ListVersions(ContentItemId id, CancellationToken c) => Task.FromResult<IReadOnlyCollection<ContentItemVersionDetails>>([]);
        public Task Add(ContentItem i, CancellationToken c) => Task.CompletedTask;
        public Task Update(ContentItem i, CancellationToken c) => Task.CompletedTask;
        public Task AddVersion(ContentItem i, CancellationToken c) { VersionedItem = i; return Task.CompletedTask; }
        public Task<bool> ExistsWithFieldValue(ContentTypeId typeId, string field, string value, ContentItemId? exclude, CancellationToken c) => Task.FromResult(false);
    }

    private sealed class FakeContentUnitOfWork : IContentUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }
        public Task SaveChanges(CancellationToken c) { SaveChangesCalls++; return Task.CompletedTask; }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
