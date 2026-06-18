namespace Mosaic.Modules.Identity.Application.Login;

public interface IPasswordPolicy
{
    void EnsureValid(string password);
}
