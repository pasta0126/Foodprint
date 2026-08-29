using System.Globalization;
using System.Security.Claims;
using Foodprint.Core.Localization;
using Microsoft.AspNetCore.Localization;

namespace Foodprint.Web.Localization;

/// <summary>
/// Resolves the UI language per request in the order the localization spec requires:
/// the authenticated user's profile language (carried as a claim by the session auth
/// handler), then a culture cookie set by the language switcher, then the request's
/// Accept-Language header, then Spanish.
/// </summary>
public static class FoodprintRequestLocalization
{
    /// <summary>Claim type the session auth handler uses to carry <c>Profile.Language</c>.</summary>
    public const string LanguageClaim = "fp:language";

    public static RequestLocalizationOptions Build()
    {
        var supported = SupportedLanguages.Cultures.ToArray();

        var options = new RequestLocalizationOptions()
            .SetDefaultCulture(SupportedLanguages.Default)
            .AddSupportedCultures(SupportedLanguages.All.ToArray())
            .AddSupportedUICultures(SupportedLanguages.All.ToArray());

        options.ApplyCurrentCultureToResponseHeaders = true;

        options.RequestCultureProviders.Clear();
        options.RequestCultureProviders.Add(new ProfileClaimCultureProvider());
        options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
        options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider
        {
            Options = options,
        });

        return options;
    }
}

/// <summary>Reads the <see cref="FoodprintRequestLocalization.LanguageClaim"/> claim.</summary>
public sealed class ProfileClaimCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var lang = httpContext.User.FindFirstValue(FoodprintRequestLocalization.LanguageClaim);
        var match = SupportedLanguages.Match(lang);
        return Task.FromResult(match is null ? null : new ProviderCultureResult(match));
    }
}
