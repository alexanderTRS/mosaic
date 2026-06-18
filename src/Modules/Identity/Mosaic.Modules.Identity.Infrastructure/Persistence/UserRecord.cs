namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class UserRecord
{
    public Guid Id { get; set; }

    public required string UserName { get; set; }

    public required string PasswordHash { get; set; }

    public bool IsAdministrator { get; set; }

    public bool CanViewGraphQLSchema { get; set; }

    public bool IsServiceAccount { get; set; }
}
