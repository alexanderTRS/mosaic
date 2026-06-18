namespace Mosaic.Modules.Identity.Application.Login;

public interface ITokenGenerator
{
    string Generate();

    string Hash(string token);
}
