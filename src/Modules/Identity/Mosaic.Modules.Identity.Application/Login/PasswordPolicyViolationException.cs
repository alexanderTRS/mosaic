namespace Mosaic.Modules.Identity.Application.Login;

public sealed class PasswordPolicyViolationException : Exception
{
    public PasswordPolicyViolationException(string message)
        : base(message)
    {
    }
}
