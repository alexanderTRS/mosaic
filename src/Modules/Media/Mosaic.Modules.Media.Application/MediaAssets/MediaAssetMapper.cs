using Mosaic.Modules.Media.Domain.MediaAssets;

namespace Mosaic.Modules.Media.Application.MediaAssets;

public static class MediaAssetMapper
{
    public static MediaAssetDetails ToDetails(MediaAsset asset)
        => new(
            asset.Id.Value,
            asset.FileName,
            asset.ContentType,
            asset.Size,
            asset.PublicUrl,
            asset.Width,
            asset.Height,
            asset.Metadata.AltText,
            asset.Metadata.LocalizedAltText,
            asset.CreatedAt,
            asset.CreatedBy);
}
