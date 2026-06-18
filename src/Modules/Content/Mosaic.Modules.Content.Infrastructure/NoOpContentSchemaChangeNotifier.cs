using Mosaic.Modules.Content.Application.ContentTypes;

namespace Mosaic.Modules.Content.Infrastructure;

public sealed class NoOpContentSchemaChangeNotifier : IContentSchemaChangeNotifier
{
    public Task PublishedContentTypesChanged(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
