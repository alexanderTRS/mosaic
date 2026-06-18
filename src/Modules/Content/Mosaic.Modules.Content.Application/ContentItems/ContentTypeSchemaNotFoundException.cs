namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class ContentTypeSchemaNotFoundException : Exception
{
    public ContentTypeSchemaNotFoundException(string contentTypeApiName)
        : base($"Content type '{contentTypeApiName}' was not found.")
    {
        ContentTypeApiName = contentTypeApiName;
    }

    public string ContentTypeApiName { get; }
}
