using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Foodprint.Core.Localization;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Profiles;

public sealed record ProfileView(string DisplayName, string TimeZoneId, string Language);

public enum ProfileError { None, InvalidName, InvalidTimeZone, InvalidLanguage, NotFound }

/// <summary>Reads and updates the per-user profile (display name, time zone, UI language).</summary>
public sealed class ProfileService(AppDbContext db)
{
    public async Task<ProfileView?> GetAsync(Guid userId, CancellationToken ct = default) =>
        await db.Profiles
            .Where(p => p.UserId == userId)
            .Select(p => new ProfileView(p.DisplayName, p.TimeZoneId, p.Language))
            .FirstOrDefaultAsync(ct);

    public async Task<ProfileError> UpdateAsync(
        Guid userId, string displayName, string timeZoneId, string language, CancellationToken ct = default)
    {
        displayName = displayName?.Trim() ?? "";
        if (displayName.Length is < 1 or > 80)
        {
            return ProfileError.InvalidName;
        }

        if (!IsKnownTimeZone(timeZoneId))
        {
            return ProfileError.InvalidTimeZone;
        }

        if (!SupportedLanguages.IsSupported(language))
        {
            return ProfileError.InvalidLanguage;
        }

        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            return ProfileError.NotFound;
        }

        profile.DisplayName = displayName;
        profile.TimeZoneId = timeZoneId;
        profile.Language = language;
        await db.SaveChangesAsync(ct);
        return ProfileError.None;
    }

    public async Task<ProfileError> SetLanguageAsync(Guid userId, string language, CancellationToken ct = default)
    {
        if (!SupportedLanguages.IsSupported(language))
        {
            return ProfileError.InvalidLanguage;
        }

        var updated = await db.Profiles
            .Where(p => p.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Language, language), ct);

        return updated == 1 ? ProfileError.None : ProfileError.NotFound;
    }

    /// <summary>Resolves the profile time zone to a <see cref="TimeZoneInfo"/>, falling back to UTC.</summary>
    public static TimeZoneInfo ResolveZone(string timeZoneId) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var tz) ? tz : TimeZoneInfo.Utc;

    public static bool IsKnownTimeZone(string? id) =>
        !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out _);
}
