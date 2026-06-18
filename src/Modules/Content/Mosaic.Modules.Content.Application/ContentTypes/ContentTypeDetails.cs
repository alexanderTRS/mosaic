using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed record ContentTypeDetails(
    Guid Id,
    string ApiName,
    string DisplayName,
    ContentTypeStatus Status,
    DateTimeOffset? PublishedAt,
    int SchemaVersion,
    IReadOnlyCollection<ContentFieldDetails> Fields);
