namespace Mosaic.Modules.Identity.Application.Login;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
