namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed class MediaAssetNotFoundException : Exception
{
    public MediaAssetNotFoundException(Guid mediaAssetId)
        : base($"Media asset '{mediaAssetId}' was not found.")
    {
        MediaAssetId = mediaAssetId;
    }

    public Guid MediaAssetId { get; }
}
