using Mosaic.Modules.Media.Domain.MediaAssets;

namespace Mosaic.Modules.Media.Application.Storage;

public interface IMediaStorage
{
    Task<StoredMediaFile> Save(
        MediaAssetId id,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<MediaFile> Open(string storagePath, string contentType, CancellationToken cancellationToken);
}
