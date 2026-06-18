namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed record UpdateMediaAssetMetadataCommand(
    Guid Id,
    string? AltText,
    IReadOnlyDictionary<string, string> LocalizedAltText);
