using HotChocolate;
using HotChocolate.Types;
using Mosaic.Modules.Search.Application.ContentSearch;

namespace Mosaic.Modules.Search.GraphQL.ContentSearch;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class SearchMutations
{
    public Task<ReindexContentSearchResult> ReindexContentSearch(
        [Service] ReindexContentSearchHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(cancellationToken);
}
