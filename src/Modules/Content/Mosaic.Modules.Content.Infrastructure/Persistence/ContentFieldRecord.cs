namespace Mosaic.Modules.Content.Infrastructure.Persistence;

public sealed class ContentFieldRecord
{
    public Guid Id { get; set; }

    public Guid ContentTypeId { get; set; }

    public required string ApiName { get; set; }

    public required string DisplayName { get; set; }

    public required string Kind { get; set; }

    public required string Localization { get; set; }

    public bool IsRequired { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public string? RegexPattern { get; set; }

    public decimal? MinNumber { get; set; }

    public decimal? MaxNumber { get; set; }

    public string? RequiredLocales { get; set; }

    public bool IsUnique { get; set; }

    public bool IsRepeatable { get; set; }

    public string? DefaultValue { get; set; }

    public string? RelationTargetContentTypeApiName { get; set; }

    public bool IsDeprecated { get; set; }
}
