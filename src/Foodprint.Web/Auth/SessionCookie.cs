namespace Foodprint.Web.Auth;

public static class SessionCookie
{
    public static void Write(HttpContext http, string token) =>
        http.Response.Cookies.Append(SessionAuth.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/",
        });

    public static void Clear(HttpContext http) =>
        http.Response.Cookies.Delete(SessionAuth.CookieName, new CookieOptions { Path = "/" });
}
