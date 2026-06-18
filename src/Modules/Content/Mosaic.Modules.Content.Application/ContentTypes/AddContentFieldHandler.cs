using Microsoft.Extensions.Logging;
using Mosaic.Modules.Content.Application.Security;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed class AddContentFieldHandler
{
    private readonly IContentTypeRepository repository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly IContentAccessService accessService;
    private readonly IAuditLog auditLog;
    private readonly ILogger<AddContentFieldHandler> logger;

    public AddContentFieldHandler(
        IContentTypeRepository repository,
        IContentUnitOfWork unitOfWork,
        IContentAccessService accessService,
        IAuditLog auditLog,
        ILogger<AddContentFieldHandler> logger)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.accessService = accessService;
        this.auditLog = auditLog;
        this.logger = logger;
    }

    public async Task Handle(AddContentFieldCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var contentType = await repository.GetById(command.ContentTypeId, cancellationToken)
            ?? throw new ContentTypeNotFoundException(command.ContentTypeId);

        await accessService.EnsureCanManageContentType(contentType.ApiName.Value, cancellationToken);

        var field = ContentField.Create(
            command.ApiName,
            command.DisplayName,
            command.Kind,
            command.Localization,
            command.IsRequired,
            command.Settings);

        contentType.AddField(field);

        await repository.Update(contentType, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentFieldAdded,
            $"{contentType.Id.Value}:{field.Id.Value}",
            $"ContentTypeApiName={contentType.ApiName.Value};FieldApiName={field.ApiName.Value};Kind={field.Kind};IsRequired={field.IsRequired}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Added field {FieldApiName} to content type {ContentTypeId}",
            field.ApiName.Value,
            contentType.Id.Value);
    }
}
