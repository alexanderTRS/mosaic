using Mosaic.Modules.Media.Application.Storage;
using Mosaic.Modules.Media.Domain.MediaAssets;

namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed class OpenMediaAssetFileHandler
{
    private readonly IMediaAssetRepository repository;
    private readonly IMediaStorage storage;

    public OpenMediaAssetFileHandler(IMediaAssetRepository repository, IMediaStorage storage)
    {
        this.repository = repository;
        this.storage = storage;
    }

    public async Task<MediaAssetFile> Handle(Guid id, CancellationToken cancellationToken)
    {
        var asset = await repository.Get(MediaAssetId.From(id), cancellationToken)
            ?? throw new MediaAssetNotFoundException(id);
        var file = await storage.Open(asset.StoragePath, asset.ContentType, cancellationToken);

        return new MediaAssetFile(file.Content, file.ContentType, asset.FileName);
    }
}
