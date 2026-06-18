namespace Mosaic.Api.Auth;

public sealed record LoginRequest(string UserName, string Password, string? ReturnUrl);
