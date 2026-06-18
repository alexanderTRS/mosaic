using System.Text.Json;
using System.Text.Json.Nodes;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.ContentItems;

public sealed class ContentItem : Entity<ContentItemId>
{
    private ContentItem(
        ContentItemId id,
        ContentTypeId contentTypeId,
        ContentItemStatus status,
        string data,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ContentTypeId = contentTypeId;
        Status = status;
        Data = data;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public ContentTypeId ContentTypeId { get; }

    public ContentItemStatus Status { get; private set; }

    public string Data { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ContentItem Create(
        ContentType contentType,
        string data,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        if (contentType.Status != ContentTypeStatus.Published)
        {
            throw new DomainRuleViolationException("Content items can be created only for published content types.");
        }

        var normalizedData = NormalizeAndValidate(contentType, data);

        return new ContentItem(
            ContentItemId.New(),
            contentType.Id,
            ContentItemStatus.Draft,
            normalizedData,
            now,
            now);
    }

    public static ContentItem Restore(
        ContentItemId id,
        ContentTypeId contentTypeId,
        ContentItemStatus status,
        string data,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, contentTypeId, status, data, createdAt, updatedAt);

    public void Update(ContentType contentType, string data, DateTimeOffset now)
    {
        EnsureCanEdit();
        Data = NormalizeAndValidate(contentType, data);
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        EnsureCanEdit();
        Status = ContentItemStatus.Published;
        UpdatedAt = now;
    }

    public void Unpublish(DateTimeOffset now)
    {
        EnsureCanEdit();
        Status = ContentItemStatus.Draft;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        if (Status == ContentItemStatus.Archived)
        {
            return;
        }

        Status = ContentItemStatus.Archived;
        UpdatedAt = now;
    }

    private static string NormalizeAndValidate(ContentType contentType, string data)
    {
        var normalizedData = ApplyDefaultValues(contentType, data);

        using var document = JsonDocument.Parse(normalizedData);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new DomainRuleViolationException("Content item data must be a JSON object.");
        }

        foreach (var requiredField in contentType.Fields.Where(field => field.IsRequired))
        {
            if (!document.RootElement.TryGetProperty(requiredField.ApiName.Value, out var value)
                || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new DomainRuleViolationException(
                    $"Required field '{requiredField.ApiName}' is missing.");
            }
        }

        foreach (var field in contentType.Fields)
        {
            if (document.RootElement.TryGetProperty(field.ApiName.Value, out var value))
            {
                ContentFields.ContentFieldValueValidator.Validate(field, value);
            }
        }

        return normalizedData;
    }

    private void EnsureCanEdit()
    {
        if (Status == ContentItemStatus.Archived)
        {
            throw new DomainRuleViolationException("Archived content items cannot be changed.");
        }
    }

    private static string ApplyDefaultValues(ContentType contentType, string data)
    {
        var node = JsonNode.Parse(data);
        if (node is not JsonObject jsonObject)
        {
            return data;
        }

        foreach (var field in contentType.Fields.Where(field => field.Settings.DefaultValue is not null))
        {
            if (!jsonObject.ContainsKey(field.ApiName.Value))
            {
                jsonObject[field.ApiName.Value] = JsonNode.Parse(field.Settings.DefaultValue!);
            }
        }

        return jsonObject.ToJsonString();
    }
}
