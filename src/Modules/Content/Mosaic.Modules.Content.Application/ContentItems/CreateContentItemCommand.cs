namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed record CreateContentItemCommand(string ContentTypeApiName, string Data);
