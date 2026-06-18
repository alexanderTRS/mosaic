using Mosaic.Modules.Content.Domain.ContentFields;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed record ContentFieldDetails(
    Guid Id,
    string ApiName,
    string DisplayName,
    FieldKind Kind,
    LocalizationMode Localization,
    bool IsRequired,
    ContentFieldSettings Settings,
    bool IsDeprecated);
