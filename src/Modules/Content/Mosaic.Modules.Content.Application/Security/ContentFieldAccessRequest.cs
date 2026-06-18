namespace Mosaic.Modules.Content.Application.Security;

public sealed record ContentFieldAccessRequest(
    string FieldApiName,
    string? Locale);
