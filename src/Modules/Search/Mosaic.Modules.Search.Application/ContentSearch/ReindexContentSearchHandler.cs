using Microsoft.Extensions.Logging;
using Mosaic.Modules.Search.Application.Security;
using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Search.Application.ContentSearch;

public sealed class ReindexContentSearchHandler
{
    private readonly IContentSearchRepository repository;
    private readonly ISearchAccessService accessService;
    private readonly IAuditLog auditLog;
    private readonly ILogger<ReindexContentSearchHandler> logger;

    public ReindexContentSearchHandler(
        IContentSearchRepository repository,
        ISearchAccessService accessService,
        IAuditLog auditLog,
        ILogger<ReindexContentSearchHandler> logger)
    {
        this.repository = repository;
        this.accessService = accessService;
        this.auditLog = auditLog;
        this.logger = logger;
    }

    public async Task<ReindexContentSearchResult> Handle(CancellationToken cancellationToken)
    {
        await accessService.EnsureCanManageSearch(cancellationToken);

        var indexedCount = await repository.Rebuild(cancellationToken);
        await auditLog.Record(
            AuditAction.SearchContentReindexed,
            "content_items",
            $"IndexedCount={indexedCount}",
            cancellationToken);

        logger.LogInformation("Rebuilt content search index with {IndexedCount} documents", indexedCount);

        return new ReindexContentSearchResult(indexedCount);
    }
}
