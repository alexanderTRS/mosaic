using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class EfAuditLog : IAuditLog
{
    private readonly IdentityDbContext dbContext;
    private readonly ICurrentUserAccessor currentUserAccessor;

    public EfAuditLog(IdentityDbContext dbContext, ICurrentUserAccessor currentUserAccessor)
    {
        this.dbContext = dbContext;
        this.currentUserAccessor = currentUserAccessor;
    }

    public async Task Record(
        string action,
        string subject,
        string? details,
        CancellationToken cancellationToken)
    {
        var user = currentUserAccessor.CurrentUser;
        await dbContext.AuditEvents.AddAsync(
            new AuditEventRecord
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                ActorUserId = user.IsAuthenticated ? user.Id : null,
                ActorUserName = user.IsAuthenticated ? user.UserName : null,
                Action = action,
                Subject = subject,
                Details = details
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
