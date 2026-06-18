namespace Mosaic.Modules.Content.Application.ContentTypes;

public interface IContentSchemaChangeNotifier
{
    Task PublishedContentTypesChanged(CancellationToken cancellationToken);
}
