namespace Mosaic.Modules.Search.Application.ContentSearch;

public interface IContentSearchRepository
{
    Task<SearchContentItemsPage> Search(SearchContentItemsQuery query, CancellationToken cancellationToken);

    Task<int> Rebuild(CancellationToken cancellationToken);
}
