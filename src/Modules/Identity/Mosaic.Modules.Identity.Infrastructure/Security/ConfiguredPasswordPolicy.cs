using Microsoft.Extensions.Options;
using Mosaic.Modules.Identity.Application.Login;

namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class ConfiguredPasswordPolicy : IPasswordPolicy
{
    private readonly IdentitySecurityOptions options;

    public ConfiguredPasswordPolicy(IOptions<IdentitySecurityOptions> options)
    {
        this.options = options.Value;
    }

    public void EnsureValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new PasswordPolicyViolationException("Password is required.");
        }

        if (password.Length < options.MinimumPasswordLength)
        {
            throw new PasswordPolicyViolationException(
                $"Password must be at least {options.MinimumPasswordLength} characters long.");
        }

        if (options.RequirePasswordDigit && !password.Any(char.IsDigit))
        {
            throw new PasswordPolicyViolationException("Password must contain at least one digit.");
        }

        if (options.RequirePasswordUppercase && !password.Any(char.IsUpper))
        {
            throw new PasswordPolicyViolationException("Password must contain at least one uppercase letter.");
        }
    }
}
