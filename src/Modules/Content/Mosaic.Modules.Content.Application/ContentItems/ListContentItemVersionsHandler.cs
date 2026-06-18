using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class ListContentItemVersionsHandler
{
    private readonly IContentItemRepository itemRepository;
    private readonly IContentTypeRepository contentTypeRepository;
    private readonly IContentAccessService accessService;

    public ListContentItemVersionsHandler(
        IContentItemRepository itemRepository,
        IContentTypeRepository contentTypeRepository,
        IContentAccessService accessService)
    {
        this.itemRepository = itemRepository;
        this.contentTypeRepository = contentTypeRepository;
        this.accessService = accessService;
    }

    public async Task<IReadOnlyCollection<ContentItemVersionDetails>> Handle(
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        var item = await itemRepository.GetDomainById(contentItemId, cancellationToken)
            ?? throw new ContentItemNotFoundException(contentItemId);
        var contentType = await contentTypeRepository.GetById(item.ContentTypeId, cancellationToken)
            ?? throw new ContentTypeNotFoundException(item.ContentTypeId);

        await accessService.EnsureCanReadContentItems(contentType.ApiName.Value, cancellationToken);

        return await itemRepository.ListVersions(contentItemId, cancellationToken);
    }
}
