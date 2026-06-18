using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed record ChangeContentItemStatusCommand(ContentItemId ContentItemId);
