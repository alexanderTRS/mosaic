using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed class ContentTypeNotFoundException : Exception
{
    public ContentTypeNotFoundException(ContentTypeId contentTypeId)
        : base($"Content type '{contentTypeId.Value}' was not found.")
    {
        ContentTypeId = contentTypeId;
    }

    public ContentTypeId ContentTypeId { get; }
}
