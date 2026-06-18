using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.Infrastructure.Persistence;

public sealed class ContentTypeRepository : IContentTypeRepository
{
    private readonly ContentDbContext dbContext;

    public ContentTypeRepository(ContentDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ContentType?> GetById(ContentTypeId contentTypeId, CancellationToken cancellationToken)
    {
        var record = await dbContext.ContentTypes
            .AsNoTracking()
            .Include(contentType => contentType.Fields)
            .SingleOrDefaultAsync(
                contentType => contentType.Id == contentTypeId.Value,
                cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<ContentType?> GetByApiName(string apiName, CancellationToken cancellationToken)
    {
        var record = await dbContext.ContentTypes
            .AsNoTracking()
            .Include(contentType => contentType.Fields)
            .SingleOrDefaultAsync(
                contentType => contentType.ApiName == apiName,
                cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyCollection<ContentType>> List(CancellationToken cancellationToken)
    {
        var records = await dbContext.ContentTypes
            .AsNoTracking()
            .Include(contentType => contentType.Fields)
            .OrderBy(contentType => contentType.ApiName)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task Add(ContentType contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        await dbContext.ContentTypes.AddAsync(ToRecord(contentType), cancellationToken);
    }

    public async Task Update(ContentType contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        var existing = await dbContext.ContentTypes
            .SingleAsync(record => record.Id == contentType.Id.Value, cancellationToken);

        existing.DisplayName = contentType.DisplayName;
        existing.Status = contentType.Status.ToString();
        existing.PublishedAt = contentType.PublishedAt;
        existing.SchemaVersion = contentType.SchemaVersion;

        await dbContext.ContentFields
            .Where(field => field.ContentTypeId == contentType.Id.Value)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ContentFields.AddRangeAsync(
            contentType.Fields.Select(field => ToRecord(contentType.Id, field)),
            cancellationToken);
    }

    private static ContentType ToDomain(ContentTypeRecord record)
    {
        var fields = record.Fields.Select(field => ContentField.Restore(
            new ContentFieldId(field.Id),
            field.ApiName,
            field.DisplayName,
            Enum.Parse<FieldKind>(field.Kind),
            Enum.Parse<LocalizationMode>(field.Localization),
            field.IsRequired,
            ContentFieldSettings.Create(
                field.MinLength,
                field.MaxLength,
                field.RegexPattern,
                field.MinNumber,
                field.MaxNumber,
                DeserializeLocales(field.RequiredLocales),
                field.IsUnique,
                field.IsRepeatable,
                field.DefaultValue,
                field.RelationTargetContentTypeApiName),
            field.IsDeprecated));

        return ContentType.Restore(
            new ContentTypeId(record.Id),
            record.ApiName,
            record.DisplayName,
            Enum.Parse<ContentTypeStatus>(record.Status),
            record.PublishedAt,
            fields,
            record.SchemaVersion);
    }

    private static ContentTypeRecord ToRecord(ContentType contentType)
    {
        var record = new ContentTypeRecord
        {
            Id = contentType.Id.Value,
            ApiName = contentType.ApiName.Value,
            DisplayName = contentType.DisplayName,
            Status = contentType.Status.ToString(),
            PublishedAt = contentType.PublishedAt,
            SchemaVersion = contentType.SchemaVersion
        };

        record.Fields.AddRange(contentType.Fields.Select(field => ToRecord(contentType.Id, field)));

        return record;
    }

    private static ContentFieldRecord ToRecord(ContentTypeId contentTypeId, ContentField field)
    {
        return new ContentFieldRecord
        {
            Id = field.Id.Value,
            ContentTypeId = contentTypeId.Value,
            ApiName = field.ApiName.Value,
            DisplayName = field.DisplayName,
            Kind = field.Kind.ToString(),
            Localization = field.Localization.ToString(),
            IsRequired = field.IsRequired,
            MinLength = field.Settings.MinLength,
            MaxLength = field.Settings.MaxLength,
            RegexPattern = field.Settings.RegexPattern,
            MinNumber = field.Settings.MinNumber,
            MaxNumber = field.Settings.MaxNumber,
            RequiredLocales = field.Settings.RequiredLocales.Count == 0
                ? null
                : JsonSerializer.Serialize(field.Settings.RequiredLocales),
            IsUnique = field.Settings.IsUnique,
            IsRepeatable = field.Settings.IsRepeatable,
            DefaultValue = field.Settings.DefaultValue,
            RelationTargetContentTypeApiName = field.Settings.RelationTargetContentTypeApiName,
            IsDeprecated = field.IsDeprecated
        };
    }

    private static IReadOnlyCollection<string> DeserializeLocales(string? requiredLocales)
        => string.IsNullOrWhiteSpace(requiredLocales)
            ? []
            : JsonSerializer.Deserialize<string[]>(requiredLocales) ?? [];
}
