using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.SharedKernel.Domain;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Domain.ContentTypes;

public sealed class ContentType : Entity<ContentTypeId>
{
    private readonly List<ContentField> fields = [];

    private ContentType(
        ContentTypeId id,
        ApiName apiName,
        string displayName,
        ContentTypeStatus status,
        DateTimeOffset? publishedAt,
        int schemaVersion)
        : base(id)
    {
        ApiName = apiName;
        DisplayName = displayName;
        Status = status;
        PublishedAt = publishedAt;
        SchemaVersion = schemaVersion;
    }

    public ApiName ApiName { get; }

    public string DisplayName { get; }

    public ContentTypeStatus Status { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public int SchemaVersion { get; private set; }

    public IReadOnlyCollection<ContentField> Fields => fields.AsReadOnly();

    public static ContentType Create(string apiName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainRuleViolationException("Content type display name is required.");
        }

        return new ContentType(
            ContentTypeId.New(),
            ApiName.From(apiName),
            displayName.Trim(),
            ContentTypeStatus.Draft,
            publishedAt: null,
            schemaVersion: 1);
    }

    public static ContentType Restore(
        ContentTypeId id,
        string apiName,
        string displayName,
        ContentTypeStatus status,
        DateTimeOffset? publishedAt,
        IEnumerable<ContentField> fields,
        int schemaVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var contentType = new ContentType(
            id,
            ApiName.From(apiName),
            displayName,
            status,
            publishedAt,
            schemaVersion);

        contentType.fields.AddRange(fields);

        return contentType;
    }

    public void AddField(ContentField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (Status == ContentTypeStatus.Published)
        {
            if (field.IsRequired)
            {
                throw new DomainRuleViolationException("Only optional fields can be added to published content type schemas.");
            }
        }

        if (ReservedContentFieldNames.Contains(field.ApiName))
        {
            throw new DomainRuleViolationException($"Field name '{field.ApiName}' is reserved.");
        }

        if (fields.Any(existing => existing.ApiName == field.ApiName))
        {
            throw new DomainRuleViolationException(
                $"Field '{field.ApiName}' already exists on content type '{ApiName}'.");
        }

        fields.Add(field);
        SchemaVersion++;
    }

    public void DeprecateField(string fieldApiName)
    {
        var apiName = ApiName.From(fieldApiName);
        var field = fields.SingleOrDefault(existing => existing.ApiName == apiName)
            ?? throw new DomainRuleViolationException($"Field '{apiName}' does not exist on content type '{ApiName}'.");

        if (field.IsDeprecated)
        {
            return;
        }

        field.Deprecate();
        SchemaVersion++;
    }

    public void Publish(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == ContentTypeStatus.Published)
        {
            return;
        }

        Status = ContentTypeStatus.Published;
        PublishedAt = clock.UtcNow;
    }
}
