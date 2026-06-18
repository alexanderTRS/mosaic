using HotChocolate;
using HotChocolate.Types;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;

namespace Mosaic.Modules.Content.GraphQL.ContentTypes;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class ContentMutations
{
    public async Task<CreateContentTypePayload> CreateContentType(
        CreateContentTypeInput input,
        [Service] CreateContentTypeHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.Handle(
            new CreateContentTypeCommand(input.ApiName, input.DisplayName),
            cancellationToken);

        return new CreateContentTypePayload(id.Value);
    }

    public async Task<AddContentFieldPayload> AddContentField(
        AddContentFieldInput input,
        [Service] AddContentFieldHandler handler,
        CancellationToken cancellationToken)
    {
        var contentTypeId = new ContentTypeId(input.ContentTypeId);

        await handler.Handle(
            new AddContentFieldCommand(
                contentTypeId,
                input.ApiName,
                input.DisplayName,
                input.Kind,
                input.Localization,
                input.IsRequired,
                input.Settings?.ToDomain()),
            cancellationToken);

        return new AddContentFieldPayload(input.ContentTypeId);
    }

    public Task<ContentTypeDetails> PublishContentType(
        PublishContentTypeInput input,
        [Service] PublishContentTypeHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new PublishContentTypeCommand(new ContentTypeId(input.ContentTypeId)),
            cancellationToken);

    public Task<ContentTypeDetails> DeprecateContentField(
        DeprecateContentFieldInput input,
        [Service] DeprecateContentFieldHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new DeprecateContentFieldCommand(
                new ContentTypeId(input.ContentTypeId),
                input.FieldApiName),
            cancellationToken);

    public Task<ContentItemDetails> CreateContentItem(
        CreateContentItemInput input,
        [Service] CreateContentItemHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new CreateContentItemCommand(input.ContentTypeApiName, input.Data),
            cancellationToken);

    public Task<ContentItemDetails> UpdateContentItem(
        UpdateContentItemInput input,
        [Service] UpdateContentItemHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new UpdateContentItemCommand(new ContentItemId(input.Id), input.Data),
            cancellationToken);

    public Task<ContentItemDetails> ArchiveContentItem(
        ChangeContentItemStatusInput input,
        [Service] ArchiveContentItemHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(new ChangeContentItemStatusCommand(new ContentItemId(input.Id)), cancellationToken);

    public Task<ContentItemDetails> PublishContentItem(
        ChangeContentItemStatusInput input,
        [Service] PublishContentItemHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(new ChangeContentItemStatusCommand(new ContentItemId(input.Id)), cancellationToken);

    public Task<ContentItemDetails> UnpublishContentItem(
        ChangeContentItemStatusInput input,
        [Service] UnpublishContentItemHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(new ChangeContentItemStatusCommand(new ContentItemId(input.Id)), cancellationToken);
}

public sealed record CreateContentTypeInput(string ApiName, string DisplayName);

public sealed record CreateContentTypePayload(Guid Id);

public sealed record AddContentFieldInput(
    Guid ContentTypeId,
    string ApiName,
    string DisplayName,
    FieldKind Kind,
    LocalizationMode Localization,
    bool IsRequired,
    ContentFieldSettingsInput? Settings = null);

public sealed record ContentFieldSettingsInput(
    int? MinLength = null,
    int? MaxLength = null,
    string? RegexPattern = null,
    decimal? MinNumber = null,
    decimal? MaxNumber = null,
    IReadOnlyCollection<string>? RequiredLocales = null,
    bool IsUnique = false,
    bool IsRepeatable = false,
    string? DefaultValue = null,
    string? RelationTargetContentTypeApiName = null)
{
    public ContentFieldSettings ToDomain()
        => ContentFieldSettings.Create(
            MinLength,
            MaxLength,
            RegexPattern,
            MinNumber,
            MaxNumber,
            RequiredLocales,
            IsUnique,
            IsRepeatable,
            DefaultValue,
            RelationTargetContentTypeApiName);
}

public sealed record AddContentFieldPayload(Guid ContentTypeId);

public sealed record PublishContentTypeInput(Guid ContentTypeId);

public sealed record DeprecateContentFieldInput(Guid ContentTypeId, string FieldApiName);

public sealed record CreateContentItemInput(string ContentTypeApiName, string Data);

public sealed record UpdateContentItemInput(Guid Id, string Data);

public sealed record ChangeContentItemStatusInput(Guid Id);
