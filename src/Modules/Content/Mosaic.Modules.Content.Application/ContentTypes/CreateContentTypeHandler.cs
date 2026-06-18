using Microsoft.Extensions.Logging;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed class CreateContentTypeHandler
{
    private readonly IContentTypeRepository repository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly IContentAccessService accessService;
    private readonly IAuditLog auditLog;
    private readonly ILogger<CreateContentTypeHandler> logger;

    public CreateContentTypeHandler(
        IContentTypeRepository repository,
        IContentUnitOfWork unitOfWork,
        IContentAccessService accessService,
        IAuditLog auditLog,
        ILogger<CreateContentTypeHandler> logger)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.accessService = accessService;
        this.auditLog = auditLog;
        this.logger = logger;
    }

    public async Task<ContentTypeId> Handle(CreateContentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await accessService.EnsureCanCreateContentType(cancellationToken);

        var contentType = ContentType.Create(command.ApiName, command.DisplayName);

        await repository.Add(contentType, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentTypeCreated,
            contentType.Id.Value.ToString(),
            $"ApiName={contentType.ApiName.Value};DisplayName={contentType.DisplayName}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Created content type {ContentTypeApiName} with id {ContentTypeId}",
            contentType.ApiName.Value,
            contentType.Id.Value);

        return contentType.Id;
    }
}
