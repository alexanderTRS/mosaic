namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed record MediaAssetFile(Stream Content, string ContentType, string FileName);
