namespace Mosaic.SharedKernel.Auditing;

public interface IAuditLog
{
    Task Record(
        string action,
        string subject,
        string? details,
        CancellationToken cancellationToken);
}
