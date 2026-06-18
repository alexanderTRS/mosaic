using System.Text.Json;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class ContentItemMutationService
{
    private readonly IContentTypeRepository contentTypeRepository;
    private readonly IContentItemRepository contentItemRepository;
    private readonly IContentAccessService accessService;
    private readonly IClock clock;

    public ContentItemMutationService(
        IContentTypeRepository contentTypeRepository,
        IContentItemRepository contentItemRepository,
        IContentAccessService accessService,
        IClock clock)
    {
        this.contentTypeRepository = contentTypeRepository;
        this.contentItemRepository = contentItemRepository;
        this.accessService = accessService;
        this.clock = clock;
    }

    public async Task<(ContentItem Item, ContentType ContentType)> LoadEditable(
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        var item = await contentItemRepository.GetDomainById(contentItemId, cancellationToken)
            ?? throw new ContentItemNotFoundException(contentItemId);
        var contentType = await contentTypeRepository.GetById(item.ContentTypeId, cancellationToken)
            ?? throw new ContentTypeNotFoundException(item.ContentTypeId);

        await accessService.EnsureCanManageContentItems(contentType.ApiName.Value, cancellationToken);

        return (item, contentType);
    }

    public async Task<(ContentItem Item, ContentType ContentType)> LoadExisting(
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        var item = await contentItemRepository.GetDomainById(contentItemId, cancellationToken)
            ?? throw new ContentItemNotFoundException(contentItemId);
        var contentType = await contentTypeRepository.GetById(item.ContentTypeId, cancellationToken)
            ?? throw new ContentTypeNotFoundException(item.ContentTypeId);

        return (item, contentType);
    }

    public async Task EnsureCanManageSubmittedFields(
        ContentType contentType,
        string data,
        CancellationToken cancellationToken)
    {
        await accessService.EnsureCanManageContentFields(
            contentType.ApiName.Value,
            ExtractFieldAccessRequests(contentType, data),
            cancellationToken);
    }

    public async Task EnsureUniqueFields(
        ContentType contentType,
        ContentItem item,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(item.Data);
        foreach (var uniqueField in contentType.Fields.Where(field => field.Settings.IsUnique))
        {
            if (document.RootElement.TryGetProperty(uniqueField.ApiName.Value, out var value)
                && await contentItemRepository.ExistsWithFieldValue(
                    contentType.Id,
                    uniqueField.ApiName.Value,
                    value.GetRawText(),
                    item.Id,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Field '{uniqueField.ApiName.Value}' must be unique for content type '{contentType.ApiName.Value}'.");
            }
        }
    }

    public async Task EnsureRelationTargetsExist(
        ContentType contentType,
        ContentItem item,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(item.Data);
        foreach (var relationField in contentType.Fields.Where(field => field.Kind == FieldKind.Relation))
        {
            if (!document.RootElement.TryGetProperty(relationField.ApiName.Value, out var value)
                || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            var targetApiName = relationField.Settings.RelationTargetContentTypeApiName;
            if (string.IsNullOrWhiteSpace(targetApiName))
            {
                continue;
            }

            var targetContentType = await contentTypeRepository.GetByApiName(targetApiName, cancellationToken);
            if (targetContentType is null)
            {
                throw new InvalidOperationException(
                    $"Relation field '{relationField.ApiName.Value}' references unknown content type '{targetApiName}'.");
            }

            foreach (var referenceId in ExtractRelationIds(value, relationField.Settings.IsRepeatable))
            {
                if (!await contentItemRepository.ExistsById(
                        targetContentType.Id,
                        new ContentItemId(referenceId),
                        cancellationToken))
                {
                    throw new InvalidOperationException(
                        $"Relation field '{relationField.ApiName.Value}' references non-existent content item '{referenceId}'.");
                }
            }
        }
    }

    private static IEnumerable<Guid> ExtractRelationIds(JsonElement value, bool isRepeatable)
    {
        if (isRepeatable && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in value.EnumerateArray())
            {
                if (TryGetRelationId(element, out var id))
                {
                    yield return id;
                }
            }

            yield break;
        }

        if (TryGetRelationId(value, out var singleId))
        {
            yield return singleId;
        }
    }

    private static bool TryGetRelationId(JsonElement value, out Guid id)
    {
        id = Guid.Empty;

        if (value.ValueKind == JsonValueKind.String)
        {
            return Guid.TryParse(value.GetString(), out id);
        }

        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("id", out var idProperty)
            && idProperty.ValueKind == JsonValueKind.String)
        {
            return Guid.TryParse(idProperty.GetString(), out id);
        }

        return false;
    }

    public DateTimeOffset UtcNow => clock.UtcNow;

    private static IReadOnlyCollection<ContentFieldAccessRequest> ExtractFieldAccessRequests(
        ContentType contentType,
        string data)
    {
        using var document = JsonDocument.Parse(data);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var fields = contentType.Fields.ToDictionary(
            field => field.ApiName.Value,
            StringComparer.Ordinal);
        var requests = new List<ContentFieldAccessRequest>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (fields.TryGetValue(property.Name, out var field)
                && field.IsLocalized
                && property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var locale in property.Value.EnumerateObject())
                {
                    requests.Add(new ContentFieldAccessRequest(property.Name, locale.Name));
                }

                continue;
            }

            requests.Add(new ContentFieldAccessRequest(property.Name, null));
        }

        return requests;
    }
}
