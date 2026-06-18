using Mosaic.Modules.Media.Application.Security;
using Mosaic.Modules.Media.Domain.MediaAssets;

namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed class GetMediaAssetHandler
{
    private readonly IMediaAssetRepository repository;
    private readonly IMediaAccessService accessService;

    public GetMediaAssetHandler(IMediaAssetRepository repository, IMediaAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<MediaAssetDetails> Handle(Guid id, CancellationToken cancellationToken)
    {
        await accessService.EnsureCanReadMedia(cancellationToken);

        var asset = await repository.Get(MediaAssetId.From(id), cancellationToken)
            ?? throw new MediaAssetNotFoundException(id);

        return MediaAssetMapper.ToDetails(asset);
    }
}
