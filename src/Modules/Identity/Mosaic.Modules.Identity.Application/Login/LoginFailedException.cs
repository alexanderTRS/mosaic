namespace Mosaic.Modules.Identity.Application.Login;

public sealed class LoginFailedException : Exception
{
    public LoginFailedException()
        : base("Invalid username or password.")
    {
    }
}
