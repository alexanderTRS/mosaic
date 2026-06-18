namespace Mosaic.Modules.Content.Domain.ContentTypes;

public readonly record struct ContentTypeId(Guid Value)
{
    public static ContentTypeId New() => new(Guid.NewGuid());
}
