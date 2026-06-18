using HotChocolate;
using HotChocolate.Types;
using Mosaic.Modules.Media.Application.MediaAssets;

namespace Mosaic.Modules.Media.GraphQL.Media;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class MediaMutations
{
    public async Task<MediaAssetPayload> UploadMediaAsset(
        UploadMediaAssetInput input,
        [Service] UploadMediaAssetHandler handler,
        CancellationToken cancellationToken)
    {
        var bytes = Convert.FromBase64String(input.Base64Content);
        await using var stream = new MemoryStream(bytes);
        var asset = await handler.Handle(
            new UploadMediaAssetCommand(
                input.FileName,
                input.ContentType,
                stream,
                bytes.LongLength,
                input.Width,
                input.Height,
                input.AltText,
                ToDictionary(input.LocalizedAltText)),
            cancellationToken);

        return MediaAssetPayload.FromDetails(asset);
    }

    public async Task<MediaAssetPayload> UpdateMediaAssetMetadata(
        UpdateMediaAssetMetadataInput input,
        [Service] UpdateMediaAssetMetadataHandler handler,
        CancellationToken cancellationToken)
    {
        var asset = await handler.Handle(
            new UpdateMediaAssetMetadataCommand(
                input.Id,
                input.AltText,
                ToDictionary(input.LocalizedAltText)),
            cancellationToken);

        return MediaAssetPayload.FromDetails(asset);
    }

    private static IReadOnlyDictionary<string, string> ToDictionary(IReadOnlyCollection<LocalizedAltTextInput>? values)
        => values?.ToDictionary(value => value.Locale, value => value.AltText, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>();
}

public sealed record UploadMediaAssetInput(
    string FileName,
    string ContentType,
    string Base64Content,
    int? Width = null,
    int? Height = null,
    string? AltText = null,
    IReadOnlyCollection<LocalizedAltTextInput>? LocalizedAltText = null);

public sealed record UpdateMediaAssetMetadataInput(
    Guid Id,
    string? AltText = null,
    IReadOnlyCollection<LocalizedAltTextInput>? LocalizedAltText = null);

public sealed record LocalizedAltTextInput(string Locale, string AltText);
