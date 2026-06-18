namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class ContentTypePermissionRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string ContentTypeApiName { get; set; }

    public string? FieldApiName { get; set; }

    public string? Locale { get; set; }

    public bool CanManageSchema { get; set; }

    public bool CanManageItems { get; set; }

    public bool CanReadItems { get; set; }
}
