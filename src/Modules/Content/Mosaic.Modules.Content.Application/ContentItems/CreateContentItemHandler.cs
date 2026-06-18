using Microsoft.Extensions.Logging;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class CreateContentItemHandler
{
    private readonly IContentTypeRepository contentTypeRepository;
    private readonly IContentItemRepository contentItemRepository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IContentAccessService accessService;
    private readonly ContentItemMutationService mutationService;
    private readonly IAuditLog auditLog;
    private readonly ILogger<CreateContentItemHandler> logger;

    public CreateContentItemHandler(
        IContentTypeRepository contentTypeRepository,
        IContentItemRepository contentItemRepository,
        IContentUnitOfWork unitOfWork,
        IClock clock,
        IContentAccessService accessService,
        ContentItemMutationService mutationService,
        IAuditLog auditLog,
        ILogger<CreateContentItemHandler> logger)
    {
        this.contentTypeRepository = contentTypeRepository;
        this.contentItemRepository = contentItemRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.accessService = accessService;
        this.mutationService = mutationService;
        this.auditLog = auditLog;
        this.logger = logger;
    }

    public async Task<ContentItemDetails> Handle(
        CreateContentItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var contentType = await contentTypeRepository.GetByApiName(
            command.ContentTypeApiName,
            cancellationToken)
            ?? throw new ContentTypeSchemaNotFoundException(command.ContentTypeApiName);

        await mutationService.EnsureCanManageSubmittedFields(contentType, command.Data, cancellationToken);

        var item = ContentItem.Create(contentType, command.Data, clock.UtcNow);

        await mutationService.EnsureUniqueFields(contentType, item, cancellationToken);
        await mutationService.EnsureRelationTargetsExist(contentType, item, cancellationToken);

        await contentItemRepository.Add(item, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentItemCreated,
            item.Id.Value.ToString(),
            $"ContentTypeApiName={contentType.ApiName.Value}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Created content item {ContentItemId} for content type {ContentTypeApiName}",
            item.Id.Value,
            contentType.ApiName.Value);

        return ContentItemMapper.ToDetails(item, contentType.ApiName.Value);
    }
}
