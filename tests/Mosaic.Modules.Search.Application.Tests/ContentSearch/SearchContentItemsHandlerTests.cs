using FluentAssertions;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Search.Application.ContentSearch;

namespace Mosaic.Modules.Search.Application.Tests.ContentSearch;

public sealed class SearchContentItemsHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnEmptyPageForBlankQuery()
    {
        var repository = new StubContentSearchRepository();
        var handler = new SearchContentItemsHandler(repository, new AllowAllContentAccessService());

        var result = await handler.Handle(new SearchContentItemsQuery(" ", null, -1, 500), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Skip.Should().Be(0);
        result.Take.Should().Be(100);
        repository.SearchWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleShouldNormalizePagingAndQuery()
    {
        var repository = new StubContentSearchRepository();
        var handler = new SearchContentItemsHandler(repository, new AllowAllContentAccessService());

        await handler.Handle(new SearchContentItemsQuery(" phone ", "product", -5, 500), CancellationToken.None);

        repository.LastQuery.Should().NotBeNull();
        repository.LastQuery!.Query.Should().Be("phone");
        repository.LastQuery.Skip.Should().Be(0);
        repository.LastQuery.Take.Should().Be(100);
    }

    private sealed class StubContentSearchRepository : IContentSearchRepository
    {
        public bool SearchWasCalled { get; private set; }

        public SearchContentItemsQuery? LastQuery { get; private set; }

        public Task<SearchContentItemsPage> Search(SearchContentItemsQuery query, CancellationToken cancellationToken)
        {
            SearchWasCalled = true;
            LastQuery = query;
            return Task.FromResult(new SearchContentItemsPage(
                Array.Empty<SearchContentItemResult>(),
                Array.Empty<SearchFacetValue>(),
                Array.Empty<SearchFacetValue>(),
                0,
                query.Skip,
                query.Take));
        }

        public Task<int> Rebuild(CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    private sealed class AllowAllContentAccessService : IContentAccessService
    {
        public Task EnsureCanCreateContentType(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task EnsureCanManageContentType(string? contentTypeApiName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task EnsureCanManageContentItems(string? contentTypeApiName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task EnsureCanManageContentFields(string? contentTypeApiName, IReadOnlyCollection<ContentFieldAccessRequest> fields, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task EnsureCanReadContentItems(string? contentTypeApiName, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
