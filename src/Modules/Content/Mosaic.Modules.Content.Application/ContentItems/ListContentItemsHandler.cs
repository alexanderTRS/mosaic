using Mosaic.Modules.Content.Application.Security;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class ListContentItemsHandler
{
    private readonly IContentItemRepository repository;
    private readonly IContentAccessService accessService;

    public ListContentItemsHandler(IContentItemRepository repository, IContentAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<IReadOnlyCollection<ContentItemDetails>> Handle(
        string? contentTypeApiName,
        CancellationToken cancellationToken)
    {
        await accessService.EnsureCanReadContentItems(contentTypeApiName, cancellationToken);

        return await repository.List(contentTypeApiName, cancellationToken);
    }

    public async Task<ContentItemsPage> Handle(
        ListContentItemsQuery query,
        CancellationToken cancellationToken)
    {
        await accessService.EnsureCanReadContentItems(query.ContentTypeApiName, cancellationToken);

        return await repository.Page(query, cancellationToken);
    }
}
