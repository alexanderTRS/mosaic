namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed class ListContentTypesHandler
{
    private readonly IContentTypeRepository repository;

    public ListContentTypesHandler(IContentTypeRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyCollection<ContentTypeDetails>> Handle(CancellationToken cancellationToken)
    {
        var contentTypes = await repository.List(cancellationToken);

        return contentTypes.Select(contentType => contentType.ToDetails()).ToArray();
    }
}
