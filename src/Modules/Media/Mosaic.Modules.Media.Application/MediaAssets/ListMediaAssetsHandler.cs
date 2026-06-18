using Mosaic.Modules.Media.Application.Security;

namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed class ListMediaAssetsHandler
{
    private readonly IMediaAssetRepository repository;
    private readonly IMediaAccessService accessService;

    public ListMediaAssetsHandler(IMediaAssetRepository repository, IMediaAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<IReadOnlyList<MediaAssetDetails>> Handle(int skip, int take, CancellationToken cancellationToken)
    {
        await accessService.EnsureCanReadMedia(cancellationToken);

        var normalizedSkip = Math.Max(0, skip);
        var normalizedTake = Math.Clamp(take, 1, 100);
        var assets = await repository.List(normalizedSkip, normalizedTake, cancellationToken);

        return assets.Select(MediaAssetMapper.ToDetails).ToArray();
    }
}
