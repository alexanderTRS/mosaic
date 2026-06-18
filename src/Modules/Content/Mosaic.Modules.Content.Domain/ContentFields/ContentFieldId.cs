namespace Mosaic.Modules.Content.Domain.ContentFields;

public readonly record struct ContentFieldId(Guid Value)
{
    public static ContentFieldId New() => new(Guid.NewGuid());
}
