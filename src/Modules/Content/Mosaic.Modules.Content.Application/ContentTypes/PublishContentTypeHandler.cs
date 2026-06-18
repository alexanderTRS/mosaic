using Microsoft.Extensions.Logging;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed class PublishContentTypeHandler
{
    private readonly IContentTypeRepository repository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly IContentAccessService accessService;
    private readonly IAuditLog auditLog;
    private readonly IContentSchemaChangeNotifier schemaChangeNotifier;
    private readonly ILogger<PublishContentTypeHandler> logger;

    public PublishContentTypeHandler(
        IContentTypeRepository repository,
        IContentUnitOfWork unitOfWork,
        IClock clock,
        IContentAccessService accessService,
        IAuditLog auditLog,
        IContentSchemaChangeNotifier schemaChangeNotifier,
        ILogger<PublishContentTypeHandler> logger)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.accessService = accessService;
        this.auditLog = auditLog;
        this.schemaChangeNotifier = schemaChangeNotifier;
        this.logger = logger;
    }

    public async Task<ContentTypeDetails> Handle(
        PublishContentTypeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var contentType = await repository.GetById(command.ContentTypeId, cancellationToken)
            ?? throw new ContentTypeNotFoundException(command.ContentTypeId);

        await accessService.EnsureCanManageContentType(contentType.ApiName.Value, cancellationToken);

        contentType.Publish(clock);

        await repository.Update(contentType, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentTypePublished,
            contentType.Id.Value.ToString(),
            $"ApiName={contentType.ApiName.Value}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);
        await schemaChangeNotifier.PublishedContentTypesChanged(cancellationToken);

        logger.LogInformation(
            "Published content type {ContentTypeApiName} with id {ContentTypeId}",
            contentType.ApiName.Value,
            contentType.Id.Value);

        return contentType.ToDetails();
    }
}
