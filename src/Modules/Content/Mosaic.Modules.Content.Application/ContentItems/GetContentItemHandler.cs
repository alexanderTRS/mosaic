using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Application.Security;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class GetContentItemHandler
{
    private readonly IContentItemRepository repository;
    private readonly IContentAccessService accessService;

    public GetContentItemHandler(IContentItemRepository repository, IContentAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<ContentItemDetails> Handle(
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetById(contentItemId, cancellationToken)
            ?? throw new ContentItemNotFoundException(contentItemId);

        await accessService.EnsureCanReadContentItems(item.ContentTypeApiName, cancellationToken);

        return item;
    }
}
