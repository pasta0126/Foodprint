using Foodprint.Core.Auth;

namespace Foodprint.Web.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/sign-out", async (HttpContext http, AuthService auth) =>
        {
            if (http.Request.Cookies.TryGetValue(SessionAuth.CookieName, out var token) && !string.IsNullOrEmpty(token))
            {
                await auth.SignOutAsync(token, http.RequestAborted);
            }

            SessionCookie.Clear(http);
            return Results.LocalRedirect("/sign-in");
        }).DisableAntiforgery();

        return app;
    }
}
