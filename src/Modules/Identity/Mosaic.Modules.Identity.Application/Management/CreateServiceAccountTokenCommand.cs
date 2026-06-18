namespace Mosaic.Modules.Identity.Application.Management;

public sealed record CreateServiceAccountTokenCommand(
    Guid ServiceAccountId,
    string Name,
    int LifetimeDays);
