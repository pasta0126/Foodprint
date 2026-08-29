using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Foodprint.Core.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Foodprint.Core.Auth;

/// <summary>
/// Ensures the configured admin account exists. When it has no password yet, mints a
/// registration link and returns it so the operator can complete setup.
/// </summary>
public sealed class AdminBootstrapper(
    AppDbContext db,
    RegistrationService registration,
    TimeProvider clock,
    IOptions<FoodprintOptions> options)
{
    public async Task<string?> EnsureAsync(CancellationToken ct = default)
    {
        var email = options.Value.AdminEmail?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            user = new User { Email = email, IsAdmin = true, CreatedAt = clock.GetUtcNow().UtcDateTime };
            db.Users.Add(user);
        }

        user.IsAdmin = true;
        // Every user must have a profile; the activation form fills in the real display name.
        user.Profile ??= new Profile
        {
            User = user,
            DisplayName = email,
            TimeZoneId = options.Value.DefaultTimeZone,
            Language = SupportedLanguages.Default,
        };
        await db.SaveChangesAsync(ct);

        if (user.PasswordHash is not null)
        {
            return null;
        }

        return await registration.CreateLinkAsync(email, byAdmin: true, ct: ct);
    }

    /// <summary>Builds the absolute activation URL for a raw token.</summary>
    public string ActivationUrl(string rawToken) =>
        $"{options.Value.PublicBaseUrl.TrimEnd('/')}/activate/{rawToken}";
}
