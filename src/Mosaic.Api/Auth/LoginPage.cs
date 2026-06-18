using System.Net;

namespace Mosaic.Api.Auth;

public static class LoginPage
{
    public static IResult Render(HttpContext httpContext, string? returnUrl = null, string? error = null)
    {
        var safeReturnUrl = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(returnUrl) ? "/graphql/ui" : returnUrl);
        var rawReturnUrl = WebUtility.UrlEncode(string.IsNullOrWhiteSpace(returnUrl) ? "/graphql/ui" : returnUrl);
        var csrfToken = WebUtility.HtmlEncode(CsrfTokenService.Issue(httpContext));
        var errorMarkup = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"""<div class="error">{WebUtility.HtmlEncode(error)}</div>""";
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var oidcEnabled = configuration.GetValue<bool>("Identity:Oidc:Enabled");
        var oidcProvider = WebUtility.HtmlEncode(configuration["Identity:Oidc:Provider"] ?? "OIDC");
        var oidcMarkup = oidcEnabled
            ? $$"""
                  <a class="oidc" href="/login/oidc?returnUrl={{rawReturnUrl}}">Sign in with {{oidcProvider}}</a>
                """
            : string.Empty;

        var html = $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Mosaic Login</title>
              <style>
                :root {
                  color-scheme: light dark;
                  font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                }
                body {
                  min-height: 100vh;
                  margin: 0;
                  display: grid;
                  place-items: center;
                  background: #f4f6f8;
                  color: #16202a;
                }
                main {
                  width: min(420px, calc(100vw - 32px));
                  background: #ffffff;
                  border: 1px solid #d8dee6;
                  border-radius: 8px;
                  padding: 28px;
                  box-shadow: 0 16px 40px rgba(22, 32, 42, .08);
                }
                h1 {
                  font-size: 24px;
                  margin: 0 0 6px;
                }
                p {
                  margin: 0 0 24px;
                  color: #607080;
                }
                label {
                  display: block;
                  font-size: 14px;
                  font-weight: 600;
                  margin: 16px 0 6px;
                }
                input {
                  width: 100%;
                  box-sizing: border-box;
                  border: 1px solid #c8d0da;
                  border-radius: 6px;
                  padding: 11px 12px;
                  font: inherit;
                }
                button {
                  width: 100%;
                  border: 0;
                  border-radius: 6px;
                  margin-top: 22px;
                  padding: 12px;
                  background: #2358d7;
                  color: white;
                  font: inherit;
                  font-weight: 700;
                  cursor: pointer;
                }
                .oidc {
                  display: block;
                  box-sizing: border-box;
                  width: 100%;
                  border: 1px solid #b8c4d2;
                  border-radius: 6px;
                  margin-top: 12px;
                  padding: 11px 12px;
                  color: #16202a;
                  text-align: center;
                  text-decoration: none;
                  font-weight: 700;
                }
                .error {
                  border: 1px solid #e2a6a6;
                  background: #fff1f1;
                  color: #8a1f1f;
                  border-radius: 6px;
                  padding: 10px 12px;
                  margin-bottom: 16px;
                }
              </style>
            </head>
            <body>
              <main>
                <h1>Mosaic</h1>
                <p>Sign in to continue.</p>
                {{errorMarkup}}
                <form method="post" action="/login">
                  <input type="hidden" name="returnUrl" value="{{safeReturnUrl}}">
                  <input type="hidden" name="{{CsrfTokenService.FormFieldName}}" value="{{csrfToken}}">
                  <label for="userName">Username</label>
                  <input id="userName" name="userName" autocomplete="username" required>
                  <label for="password">Password</label>
                  <input id="password" name="password" type="password" autocomplete="current-password" required>
                  <button type="submit">Sign in</button>
                </form>
                {{oidcMarkup}}
              </main>
            </body>
            </html>
            """;

        return Results.Content(html, "text/html");
    }
}
