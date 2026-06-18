namespace Mosaic.Modules.Media.Domain.MediaAssets;

public readonly record struct MediaAssetId(Guid Value)
{
    public static MediaAssetId New() => new(Guid.NewGuid());

    public static MediaAssetId From(Guid value) => new(value);
}
