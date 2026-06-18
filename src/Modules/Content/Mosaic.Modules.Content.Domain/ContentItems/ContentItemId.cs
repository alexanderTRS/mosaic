namespace Mosaic.Modules.Content.Domain.ContentItems;

public readonly record struct ContentItemId(Guid Value)
{
    public static ContentItemId New() => new(Guid.NewGuid());
}
