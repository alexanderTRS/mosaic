using HotChocolate;
using HotChocolate.Types;
using Mosaic.Modules.Search.Application.ContentSearch;

namespace Mosaic.Modules.Search.GraphQL.ContentSearch;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class SearchQueries
{
    public string SearchModuleStatus() => "ready";

    public Task<SearchContentItemsPage> SearchContentItems(
        string query,
        string? contentTypeApiName,
        int skip,
        int take,
        [Service] SearchContentItemsHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new SearchContentItemsQuery(query, contentTypeApiName, skip, take),
            cancellationToken);
}
