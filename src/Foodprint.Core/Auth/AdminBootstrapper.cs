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

        var user = await GetOrCreateAdminAsync(email, ct);

        return user.PasswordHash is not null
            ? null
            : await registration.CreateLinkAsync(email, byAdmin: true, ct: ct);
    }

    /// <summary>
    /// Idempotent and safe if another instance is starting against the same database:
    /// a losing racer catches the unique-email violation and re-reads.
    /// </summary>
    private async Task<User> GetOrCreateAdminAsync(string email, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is not null)
            {
                var changed = !user.IsAdmin;
                user.IsAdmin = true;
                if (user.Profile is null)
                {
                    user.Profile = NewProfile(user, email);
                    changed = true;
                }

                if (changed)
                {
                    await db.SaveChangesAsync(ct);
                }

                return user;
            }

            user = new User { Email = email, IsAdmin = true, CreatedAt = clock.GetUtcNow().UtcDateTime };
            user.Profile = NewProfile(user, email);
            db.Users.Add(user);

            try
            {
                await db.SaveChangesAsync(ct);
                return user;
            }
            catch (DbUpdateException) when (attempt == 0)
            {
                // Another starting instance inserted the admin first — reload on the next pass.
                db.ChangeTracker.Clear();
            }
        }
    }

    private Profile NewProfile(User user, string email) => new()
    {
        User = user,
        DisplayName = email,
        TimeZoneId = options.Value.DefaultTimeZone,
        Language = SupportedLanguages.Default,
    };

    /// <summary>Builds the absolute activation URL for a raw token.</summary>
    public string ActivationUrl(string rawToken) =>
        $"{options.Value.PublicBaseUrl.TrimEnd('/')}/activate/{rawToken}";
}
