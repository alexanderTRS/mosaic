namespace Mosaic.SharedKernel.Security;

public sealed record CurrentUser(
    Guid? Id,
    string? UserName,
    bool IsAuthenticated,
    bool IsAdministrator,
    bool CanViewGraphQLSchema);
