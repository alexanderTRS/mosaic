using System.Security.Cryptography;
using System.Text;
using Mosaic.Modules.Identity.Application.Login;

namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class TokenGenerator : ITokenGenerator
{
    public string Generate()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
