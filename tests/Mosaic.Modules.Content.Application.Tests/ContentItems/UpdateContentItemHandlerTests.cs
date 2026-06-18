using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Application.Tests.ContentItems;

public sealed class UpdateContentItemHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_should_update_item_data_and_save()
    {
        var contentType = PublishedProductType();
        var item = ContentItem.Create(contentType, """{"title":{"ru":"Old"}}""", Now);
        var itemRepo = new FakeContentItemRepository(item);
        var typeRepo = new FakeContentTypeRepository(contentType);
        var uow = new FakeContentUnitOfWork();
        var handler = BuildHandler(typeRepo, itemRepo, uow);

        var result = await handler.Handle(
            new UpdateContentItemCommand(item.Id, """{"title":{"ru":"New"}}"""),
            CancellationToken.None);

        result.Data.Should().Contain("New");
        itemRepo.UpdatedItem.Should().BeSameAs(item);
        itemRepo.VersionedItem.Should().BeSameAs(item);
        uow.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_throw_when_item_not_found()
    {
        var typeRepo = new FakeContentTypeRepository();
        var itemRepo = new FakeContentItemRepository();
        var handler = BuildHandler(typeRepo, itemRepo, new FakeContentUnitOfWork());

        var act = async () => await handler.Handle(
            new UpdateContentItemCommand(ContentItemId.New(), """{"title":{"ru":"X"}}"""),
            CancellationToken.None);

        await act.Should().ThrowAsync<ContentItemNotFoundException>();
    }

    [Fact]
    public async Task Handle_should_create_version_snapshot_before_update()
    {
        var contentType = PublishedProductType();
        var item = ContentItem.Create(contentType, """{"title":{"ru":"Old"}}""", Now);
        var itemRepo = new FakeContentItemRepository(item);
        var handler = BuildHandler(new FakeContentTypeRepository(contentType), itemRepo, new FakeContentUnitOfWork());

        await handler.Handle(
            new UpdateContentItemCommand(item.Id, """{"title":{"ru":"New"}}"""),
            CancellationToken.None);

        itemRepo.VersionedItem.Should().NotBeNull();
    }

    private static UpdateContentItemHandler BuildHandler(
        FakeContentTypeRepository typeRepo,
        FakeContentItemRepository itemRepo,
        FakeContentUnitOfWork uow)
    {
        var clock = new FixedClock(Now.AddMinutes(1));
        var mutationService = new ContentItemMutationService(typeRepo, itemRepo, new AllowAllContentAccessService(), clock);
        return new UpdateContentItemHandler(itemRepo, uow, mutationService, new NullAuditLog());
    }

    private static ContentType PublishedProductType()
    {
        var ct = ContentType.Create("product", "Product");
        ct.AddField(ContentField.Create("title", "Title", FieldKind.String, LocalizationMode.Localized, isRequired: true));
        ct.Publish(new FixedClock(Now));
        return ct;
    }

    private sealed class FakeContentTypeRepository : IContentTypeRepository
    {
        private readonly ContentType? contentType;
        public FakeContentTypeRepository(ContentType? ct = null) => contentType = ct;
        public Task<ContentType?> GetById(ContentTypeId id, CancellationToken ct) =>
            Task.FromResult(contentType?.Id == id ? contentType : null);
        public Task<ContentType?> GetByApiName(string name, CancellationToken ct) =>
            Task.FromResult(contentType?.ApiName.Value == name ? contentType : null);
        public Task<IReadOnlyCollection<ContentType>> List(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<ContentType>>(contentType is null ? [] : [contentType]);
        public Task Add(ContentType ct, CancellationToken c) => Task.CompletedTask;
        public Task Update(ContentType ct, CancellationToken c) => Task.CompletedTask;
    }

    private sealed class FakeContentItemRepository : IContentItemRepository
    {
        private readonly ContentItem? item;
        public FakeContentItemRepository(ContentItem? item = null) => this.item = item;
        public ContentItem? UpdatedItem { get; private set; }
        public ContentItem? VersionedItem { get; private set; }
        public Task<ContentItem?> GetDomainById(ContentItemId id, CancellationToken ct) =>
            Task.FromResult(item?.Id == id ? item : null);
        public Task<ContentItemDetails?> GetById(ContentItemId id, CancellationToken ct) => Task.FromResult<ContentItemDetails?>(null);
        public Task<IReadOnlyCollection<ContentItemDetails>> List(string? type, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<ContentItemDetails>>([]);
        public Task<ContentItemsPage> Page(ListContentItemsQuery q, CancellationToken ct) =>
            Task.FromResult(new ContentItemsPage([], 0, q.Skip, q.Take));
        public Task<IReadOnlyCollection<ContentItemVersionDetails>> ListVersions(ContentItemId id, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<ContentItemVersionDetails>>([]);
        public Task Add(ContentItem i, CancellationToken ct) => Task.CompletedTask;
        public Task Update(ContentItem i, CancellationToken ct) { UpdatedItem = i; return Task.CompletedTask; }
        public Task AddVersion(ContentItem i, CancellationToken ct) { VersionedItem = i; return Task.CompletedTask; }
        public Task<bool> ExistsWithFieldValue(ContentTypeId typeId, string field, string value, ContentItemId? exclude, CancellationToken ct) =>
            Task.FromResult(false);
    }

    private sealed class FakeContentUnitOfWork : IContentUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }
        public Task SaveChanges(CancellationToken ct) { SaveChangesCalls++; return Task.CompletedTask; }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
