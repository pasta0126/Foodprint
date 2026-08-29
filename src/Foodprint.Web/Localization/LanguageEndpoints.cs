using Foodprint.Core.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Foodprint.Web.Localization;

public static class LanguageEndpoints
{
    /// <summary>
    /// POST /profile/language — switches the UI language. Sets the culture cookie so
    /// anonymous and authenticated users alike see the change immediately; group 6
    /// also persists it to <c>Profile.Language</c> for signed-in users.
    /// </summary>
    public static IEndpointRouteBuilder MapLanguageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/profile/language", async (
            HttpContext http,
            [FromForm] string language,
            [FromForm] string? returnUrl,
            IServiceProvider services) =>
        {
            var match = SupportedLanguages.Match(language);
            if (match is not null)
            {
                http.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(match)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                    });

                var persist = services.GetService<ILanguagePersistence>();
                if (persist is not null)
                {
                    await persist.PersistAsync(http, match);
                }
            }

            var target = !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? returnUrl
                : "/";
            return Results.LocalRedirect(target);
        }).DisableAntiforgery();

        return app;
    }
}

/// <summary>Implemented in group 6 to write the chosen language to the user's profile.</summary>
public interface ILanguagePersistence
{
    Task PersistAsync(HttpContext http, string language);
}
