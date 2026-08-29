using Foodprint.Core.Profiles;
using Foodprint.Web.Auth;

namespace Foodprint.Web.Localization;

/// <summary>Persists the language chosen via the switcher to the signed-in user's profile.</summary>
public sealed class ProfileLanguagePersistence(CurrentUser me, ProfileService profiles) : ILanguagePersistence
{
    public async Task PersistAsync(HttpContext http, string language)
    {
        if (me.IdOrNull is { } userId)
        {
            await profiles.SetLanguageAsync(userId, language, http.RequestAborted);
        }
    }
}
