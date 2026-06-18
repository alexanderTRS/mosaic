using HotChocolate;
using HotChocolate.Types;
using Mosaic.Modules.Media.Application.MediaAssets;

namespace Mosaic.Modules.Media.GraphQL.Media;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class MediaQueries
{
    public string MediaModuleStatus() => "ready";

    public async Task<IReadOnlyCollection<MediaAssetPayload>> MediaAssets(
        int skip,
        int take,
        [Service] ListMediaAssetsHandler handler,
        CancellationToken cancellationToken)
    {
        var assets = await handler.Handle(skip, take, cancellationToken);
        return assets.Select(MediaAssetPayload.FromDetails).ToArray();
    }

    public async Task<MediaAssetPayload> MediaAsset(
        Guid id,
        [Service] GetMediaAssetHandler handler,
        CancellationToken cancellationToken)
    {
        var asset = await handler.Handle(id, cancellationToken);
        return MediaAssetPayload.FromDetails(asset);
    }
}
