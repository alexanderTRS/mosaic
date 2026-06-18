using System.Security.Cryptography;

namespace Mosaic.Api.Auth;

public static class CsrfTokenService
{
    public const string CookieName = "Mosaic.Csrf";
    public const string FormFieldName = "csrfToken";

    public static string Issue(HttpContext httpContext)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        httpContext.Response.Cookies.Append(
            CookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = httpContext.Request.IsHttps,
                Path = "/login"
            });

        return token;
    }

    public static bool IsValid(HttpContext httpContext, string? submittedToken)
    {
        if (string.IsNullOrWhiteSpace(submittedToken)
            || !httpContext.Request.Cookies.TryGetValue(CookieName, out var cookieToken))
        {
            return false;
        }

        byte[] submittedBytes;
        byte[] cookieBytes;

        try
        {
            submittedBytes = Convert.FromBase64String(submittedToken);
            cookieBytes = Convert.FromBase64String(cookieToken);
        }
        catch (FormatException)
        {
            return false;
        }

        return submittedBytes.Length == cookieBytes.Length
            && CryptographicOperations.FixedTimeEquals(submittedBytes, cookieBytes);
    }
}
