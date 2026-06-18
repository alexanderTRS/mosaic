namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed record MediaAssetDetails(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    string? PublicUrl,
    int? Width,
    int? Height,
    string? AltText,
    IReadOnlyDictionary<string, string> LocalizedAltText,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy);
