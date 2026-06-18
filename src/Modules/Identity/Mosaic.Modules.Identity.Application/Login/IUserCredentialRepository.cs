namespace Mosaic.Modules.Identity.Application.Login;

public interface IUserCredentialRepository
{
    Task<UserCredentials?> GetByUserName(string userName, CancellationToken cancellationToken);

    Task AddAccessToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    Task SaveChanges(CancellationToken cancellationToken);
}
