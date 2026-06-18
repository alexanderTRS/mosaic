namespace Mosaic.Modules.Content.Application.ContentTypes;

internal static class ContentTypeMapper
{
    public static ContentTypeDetails ToDetails(this Domain.ContentTypes.ContentType contentType)
        => new(
            contentType.Id.Value,
            contentType.ApiName.Value,
            contentType.DisplayName,
            contentType.Status,
            contentType.PublishedAt,
            contentType.SchemaVersion,
            contentType.Fields
                .Select(field => new ContentFieldDetails(
                    field.Id.Value,
                    field.ApiName.Value,
                    field.DisplayName,
                    field.Kind,
                    field.Localization,
                    field.IsRequired,
                    field.Settings,
                    field.IsDeprecated))
                .ToArray());
}
