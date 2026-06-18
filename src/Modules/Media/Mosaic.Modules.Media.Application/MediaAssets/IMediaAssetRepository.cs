using Mosaic.Modules.Media.Domain.MediaAssets;

namespace Mosaic.Modules.Media.Application.MediaAssets;

public interface IMediaAssetRepository
{
    Task Add(MediaAsset asset, CancellationToken cancellationToken);

    Task Update(MediaAsset asset, CancellationToken cancellationToken);

    Task<MediaAsset?> Get(MediaAssetId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MediaAsset>> List(int skip, int take, CancellationToken cancellationToken);
}
