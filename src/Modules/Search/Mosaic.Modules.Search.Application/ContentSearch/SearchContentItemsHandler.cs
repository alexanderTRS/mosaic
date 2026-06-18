using Mosaic.Modules.Content.Application.Security;

namespace Mosaic.Modules.Search.Application.ContentSearch;

public sealed class SearchContentItemsHandler
{
    private readonly IContentSearchRepository repository;
    private readonly IContentAccessService contentAccessService;

    public SearchContentItemsHandler(
        IContentSearchRepository repository,
        IContentAccessService contentAccessService)
    {
        this.repository = repository;
        this.contentAccessService = contentAccessService;
    }

    public async Task<SearchContentItemsPage> Handle(SearchContentItemsQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new SearchContentItemsPage(
                Array.Empty<SearchContentItemResult>(),
                Array.Empty<SearchFacetValue>(),
                Array.Empty<SearchFacetValue>(),
                0,
                Math.Max(0, query.Skip),
                Math.Clamp(query.Take, 1, 100));
        }

        await contentAccessService.EnsureCanReadContentItems(query.ContentTypeApiName, cancellationToken);

        return await repository.Search(
            query with
            {
                Query = query.Query.Trim(),
                Skip = Math.Max(0, query.Skip),
                Take = Math.Clamp(query.Take, 1, 100)
            },
            cancellationToken);
    }
}
