using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class ContentItemNotFoundException : Exception
{
    public ContentItemNotFoundException(ContentItemId contentItemId)
        : base($"Content item '{contentItemId.Value}' was not found.")
    {
        ContentItemId = contentItemId;
    }

    public ContentItemId ContentItemId { get; }
}
