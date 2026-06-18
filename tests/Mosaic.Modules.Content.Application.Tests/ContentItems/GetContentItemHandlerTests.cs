using FluentAssertions;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Content.Application.Tests.ContentItems;

public sealed class GetContentItemHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_should_return_item_when_found()
    {
        var itemId = ContentItemId.New();
        var details = new ContentItemDetails(itemId.Value, Guid.NewGuid(), "product",
            ContentItemStatus.Published, """{"title":"Test"}""", Now, Now);
        var repo = new FakeRepo(details);
        var handler = new GetContentItemHandler(repo, new AllowAllContentAccessService());

        var result = await handler.Handle(itemId, CancellationToken.None);

        result.Id.Should().Be(itemId.Value);
        result.ContentTypeApiName.Should().Be("product");
    }

    [Fact]
    public async Task Handle_should_throw_when_item_not_found()
    {
        var handler = new GetContentItemHandler(new FakeRepo(null), new AllowAllContentAccessService());
        var act = async () => await handler.Handle(ContentItemId.New(), CancellationToken.None);
        await act.Should().ThrowAsync<ContentItemNotFoundException>();
    }

    [Fact]
    public async Task Handle_should_throw_when_access_denied()
    {
        var itemId = ContentItemId.New();
        var details = new ContentItemDetails(itemId.Value, Guid.NewGuid(), "product",
            ContentItemStatus.Published, "{}", Now, Now);
        var repo = new FakeRepo(details);
        var handler = new GetContentItemHandler(repo, new DenyAllContentAccessService());

        var act = async () => await handler.Handle(itemId, CancellationToken.None);
        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    private sealed class FakeRepo : IContentItemRepository
    {
        private readonly ContentItemDetails? details;
        public FakeRepo(ContentItemDetails? details) => this.details = details;
        public Task<ContentItem?> GetDomainById(ContentItemId id, CancellationToken ct) => Task.FromResult<ContentItem?>(null);
        public Task<ContentItemDetails?> GetById(ContentItemId id, CancellationToken ct) => Task.FromResult(details);
        public Task<IReadOnlyCollection<ContentItemDetails>> List(string? type, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ContentItemDetails>>([]);
        public Task<ContentItemsPage> Page(ListContentItemsQuery q, CancellationToken ct) => Task.FromResult(new ContentItemsPage([], 0, q.Skip, q.Take));
        public Task<IReadOnlyCollection<ContentItemVersionDetails>> ListVersions(ContentItemId id, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ContentItemVersionDetails>>([]);
        public Task Add(ContentItem item, CancellationToken ct) => Task.CompletedTask;
        public Task Update(ContentItem item, CancellationToken ct) => Task.CompletedTask;
        public Task AddVersion(ContentItem item, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ExistsWithFieldValue(ContentTypeId typeId, string field, string value, ContentItemId? exclude, CancellationToken ct) => Task.FromResult(false);
    }

    private sealed class DenyAllContentAccessService : IContentAccessService
    {
        public Task EnsureCanCreateContentType(CancellationToken ct) => throw new AccessDeniedException("denied");
        public Task EnsureCanManageContentType(string? name, CancellationToken ct) => throw new AccessDeniedException("denied");
        public Task EnsureCanManageContentItems(string? name, CancellationToken ct) => throw new AccessDeniedException("denied");
        public Task EnsureCanManageContentFields(string? name, IReadOnlyCollection<ContentFieldAccessRequest> fields, CancellationToken ct) => throw new AccessDeniedException("denied");
        public Task EnsureCanReadContentItems(string? name, CancellationToken ct) => throw new AccessDeniedException("denied");
    }
}
