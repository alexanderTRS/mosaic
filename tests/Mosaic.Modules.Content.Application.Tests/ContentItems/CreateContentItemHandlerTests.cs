using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaic.Modules.Content.Application;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Application.Tests.ContentItems;

public sealed class CreateContentItemHandlerTests
{
    [Fact]
    public async Task Handle_should_create_item_for_published_content_type()
    {
        var contentType = PublishedProductType();
        var itemRepository = new FakeContentItemRepository();
        var unitOfWork = new FakeContentUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero));
        var handler = new CreateContentItemHandler(
            new FakeContentTypeRepository(contentType),
            itemRepository,
            unitOfWork,
            clock,
            new AllowAllContentAccessService(),
            new ContentItemMutationService(
                new FakeContentTypeRepository(contentType),
                itemRepository,
                new AllowAllContentAccessService(),
                clock),
            new NullAuditLog(),
            NullLogger<CreateContentItemHandler>.Instance);

        var result = await handler.Handle(
            new CreateContentItemCommand("product", """{"title":{"ru":"iPhone 15"}}"""),
            CancellationToken.None);

        result.ContentTypeApiName.Should().Be("product");
        result.Data.Should().Be("""{"title":{"ru":"iPhone 15"}}""");
        itemRepository.AddedItem.Should().NotBeNull();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_throw_when_content_type_does_not_exist()
    {
        var itemRepository = new FakeContentItemRepository();
        var contentTypeRepository = new FakeContentTypeRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var handler = new CreateContentItemHandler(
            contentTypeRepository,
            itemRepository,
            new FakeContentUnitOfWork(),
            clock,
            new AllowAllContentAccessService(),
            new ContentItemMutationService(
                contentTypeRepository,
                itemRepository,
                new AllowAllContentAccessService(),
                clock),
            new NullAuditLog(),
            NullLogger<CreateContentItemHandler>.Instance);

        var act = async () => await handler.Handle(
            new CreateContentItemCommand("missing", "{}"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ContentTypeSchemaNotFoundException>();
    }

    [Fact]
    public async Task Handle_should_throw_when_unique_field_value_already_exists()
    {
        var contentType = ContentType.Create("product", "Product");
        contentType.AddField(ContentField.Create(
            "slug",
            "Slug",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: true,
            ContentFieldSettings.Create(isUnique: true)));
        contentType.Publish(new FixedClock(DateTimeOffset.UtcNow));
        var itemRepository = new FakeContentItemRepository { UniqueValueExists = true };
        var contentTypeRepository = new FakeContentTypeRepository(contentType);
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var handler = new CreateContentItemHandler(
            contentTypeRepository,
            itemRepository,
            new FakeContentUnitOfWork(),
            clock,
            new AllowAllContentAccessService(),
            new ContentItemMutationService(
                contentTypeRepository,
                itemRepository,
                new AllowAllContentAccessService(),
                clock),
            new NullAuditLog(),
            NullLogger<CreateContentItemHandler>.Instance);

        var act = async () => await handler.Handle(
            new CreateContentItemCommand("product", """{"slug":"phone"}"""),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
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

    private sealed class FakeContentTypeRepository : IContentTypeRepository
    {
        private readonly ContentType? contentType;

        public FakeContentTypeRepository(ContentType? contentType = null)
        {
            this.contentType = contentType;
        }

        public Task<ContentType?> GetById(ContentTypeId contentTypeId, CancellationToken cancellationToken)
            => Task.FromResult(contentType?.Id == contentTypeId ? contentType : null);

        public Task<ContentType?> GetByApiName(string apiName, CancellationToken cancellationToken)
            => Task.FromResult(contentType?.ApiName.Value == apiName ? contentType : null);

        public Task<IReadOnlyCollection<ContentType>> List(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ContentType>>(
                contentType is null ? [] : [contentType]);

        public Task Add(ContentType contentType, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task Update(ContentType contentType, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeContentItemRepository : IContentItemRepository
    {
        public ContentItem? AddedItem { get; private set; }

        public bool UniqueValueExists { get; init; }

        public Task<ContentItem?> GetDomainById(ContentItemId contentItemId, CancellationToken cancellationToken)
            => Task.FromResult<ContentItem?>(null);

        public Task<ContentItemDetails?> GetById(ContentItemId contentItemId, CancellationToken cancellationToken)
            => Task.FromResult<ContentItemDetails?>(null);

        public Task<IReadOnlyCollection<ContentItemDetails>> List(
            string? contentTypeApiName,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ContentItemDetails>>([]);

        public Task<ContentItemsPage> Page(ListContentItemsQuery query, CancellationToken cancellationToken)
            => Task.FromResult(new ContentItemsPage([], 0, query.Skip, query.Take));

        public Task<IReadOnlyCollection<ContentItemVersionDetails>> ListVersions(
            ContentItemId contentItemId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ContentItemVersionDetails>>([]);

        public Task Add(ContentItem contentItem, CancellationToken cancellationToken)
        {
            AddedItem = contentItem;
            return Task.CompletedTask;
        }

        public Task Update(ContentItem contentItem, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AddVersion(ContentItem contentItem, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<bool> ExistsWithFieldValue(
            ContentTypeId contentTypeId,
            string fieldApiName,
            string fieldValueJson,
            ContentItemId? excludeContentItemId,
            CancellationToken cancellationToken)
            => Task.FromResult(UniqueValueExists);
    }

    private sealed class FakeContentUnitOfWork : IContentUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task SaveChanges(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
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
