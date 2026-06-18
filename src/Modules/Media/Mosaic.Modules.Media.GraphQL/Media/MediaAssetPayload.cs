using Mosaic.Modules.Media.Application.MediaAssets;

namespace Mosaic.Modules.Media.GraphQL.Media;

public sealed record MediaAssetPayload(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    string? PublicUrl,
    int? Width,
    int? Height,
    string? AltText,
    IReadOnlyCollection<LocalizedAltTextPayload> LocalizedAltText,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy)
{
    public static MediaAssetPayload FromDetails(MediaAssetDetails details)
        => new(
            details.Id,
            details.FileName,
            details.ContentType,
            details.Size,
            details.PublicUrl,
            details.Width,
            details.Height,
            details.AltText,
            details.LocalizedAltText
                .Select(item => new LocalizedAltTextPayload(item.Key, item.Value))
                .OrderBy(item => item.Locale, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            details.CreatedAt,
            details.CreatedBy);
}

public sealed record LocalizedAltTextPayload(string Locale, string AltText);
