using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaic.Modules.Search.Application.Security;
using Mosaic.SharedKernel.Auditing;
using Mosaic.Modules.Search.Application.ContentSearch;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Search.Application.Tests.ContentSearch;

public sealed class ReindexContentSearchHandlerTests
{
    [Fact]
    public async Task Handle_should_call_rebuild_and_return_count()
    {
        var repo = new StubRepo(indexedCount: 42);
        var auditLog = new StubAuditLog();
        var handler = new ReindexContentSearchHandler(
            repo,
            new AdminAccessService(),
            auditLog,
            NullLogger<ReindexContentSearchHandler>.Instance);

        var result = await handler.Handle(CancellationToken.None);

        result.IndexedCount.Should().Be(42);
        repo.RebuildCalled.Should().BeTrue();
        auditLog.Records.Should().ContainSingle(record =>
            record.Action == AuditAction.SearchContentReindexed
            && record.Subject == "content_items"
            && record.Details == "IndexedCount=42");
    }

    [Fact]
    public async Task Handle_should_throw_when_caller_is_not_admin()
    {
        var handler = new ReindexContentSearchHandler(
            new StubRepo(0),
            new DenyAccessService(),
            new StubAuditLog(),
            NullLogger<ReindexContentSearchHandler>.Instance);

        var act = async () => await handler.Handle(CancellationToken.None);
        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task Handle_should_return_zero_when_no_documents()
    {
        var repo = new StubRepo(indexedCount: 0);
        var handler = new ReindexContentSearchHandler(
            repo,
            new AdminAccessService(),
            new StubAuditLog(),
            NullLogger<ReindexContentSearchHandler>.Instance);

        var result = await handler.Handle(CancellationToken.None);

        result.IndexedCount.Should().Be(0);
    }

    private sealed class StubRepo : IContentSearchRepository
    {
        private readonly int indexedCount;
        public StubRepo(int indexedCount) => this.indexedCount = indexedCount;
        public bool RebuildCalled { get; private set; }
        public Task<SearchContentItemsPage> Search(SearchContentItemsQuery query, CancellationToken ct) =>
            Task.FromResult(new SearchContentItemsPage([], [], [], 0, 0, 10));
        public Task<int> Rebuild(CancellationToken ct) { RebuildCalled = true; return Task.FromResult(indexedCount); }
    }

    private sealed class AdminAccessService : ISearchAccessService
    {
        public Task EnsureCanManageSearch(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class DenyAccessService : ISearchAccessService
    {
        public Task EnsureCanManageSearch(CancellationToken ct) => throw new AccessDeniedException("denied");
    }

    private sealed class StubAuditLog : IAuditLog
    {
        public List<(string Action, string Subject, string? Details)> Records { get; } = [];

        public Task Record(string action, string subject, string? details, CancellationToken cancellationToken)
        {
            Records.Add((action, subject, details));
            return Task.CompletedTask;
        }
    }
}
