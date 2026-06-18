using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Content.Application.Tests;

internal sealed class NullAuditLog : IAuditLog
{
    public Task Record(
        string action,
        string subject,
        string? details,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
