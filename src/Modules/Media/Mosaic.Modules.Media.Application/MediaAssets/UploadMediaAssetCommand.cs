namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed record UploadMediaAssetCommand(
    string FileName,
    string ContentType,
    Stream Content,
    long Size,
    int? Width,
    int? Height,
    string? AltText,
    IReadOnlyDictionary<string, string> LocalizedAltText);
