using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public interface IContentTypeRepository
{
    Task<ContentType?> GetById(ContentTypeId contentTypeId, CancellationToken cancellationToken);

    Task<ContentType?> GetByApiName(string apiName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ContentType>> List(CancellationToken cancellationToken);

    Task Add(ContentType contentType, CancellationToken cancellationToken);

    Task Update(ContentType contentType, CancellationToken cancellationToken);
}
