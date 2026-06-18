using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed record AddContentFieldCommand(
    ContentTypeId ContentTypeId,
    string ApiName,
    string DisplayName,
    FieldKind Kind,
    LocalizationMode Localization,
    bool IsRequired,
    ContentFieldSettings? Settings = null);
