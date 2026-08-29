using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Foodprint.Core.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Foodprint.Core.Auth;

public sealed record RegistrationLinkInfo(Guid Id, string Email, DateTime ExpiresAt, DateTime? UsedAt, DateTime? RevokedAt, bool CreatedByAdmin);

/// <summary>Issues and redeems the one-time links that let a person set their password.</summary>
public sealed class RegistrationService(
    AppDbContext db,
    IPasswordHasher hasher,
    TimeProvider clock,
    IOptions<FoodprintOptions> options)
{
    private FoodprintOptions Opt => options.Value;

    /// <summary>
    /// Creates a link for <paramref name="email"/>. Returns the raw token (shown once) or null
    /// when the address already has an activated account and the caller is not an admin.
    /// </summary>
    public async Task<string?> CreateLinkAsync(string email, bool byAdmin, DateTime? expiresAt = null, CancellationToken ct = default)
    {
        email = Normalize(email);
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is { PasswordHash: not null } && !byAdmin)
        {
            return null;
        }

        var raw = Tokens.Generate();
        db.RegistrationLinks.Add(new RegistrationLink
        {
            Email = email,
            TokenHash = Tokens.Hash(raw),
            ExpiresAt = expiresAt ?? clock.GetUtcNow().UtcDateTime.AddDays(Opt.RegistrationLinkExpiryDays),
            CreatedByAdmin = byAdmin,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(ct);
        return raw;
    }

    public async Task<IReadOnlyList<RegistrationLinkInfo>> ListAsync(CancellationToken ct = default) =>
        await db.RegistrationLinks
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RegistrationLinkInfo(r.Id, r.Email, r.ExpiresAt, r.UsedAt, r.RevokedAt, r.CreatedByAdmin))
            .ToListAsync(ct);

    public async Task<bool> RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var link = await db.RegistrationLinks.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (link is null || link.RevokedAt is not null)
        {
            return false;
        }

        link.RevokedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>True when the token maps to a link that can still be redeemed.</summary>
    public async Task<bool> IsRedeemableAsync(string rawToken, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = Tokens.Hash(rawToken);
        var link = await db.RegistrationLinks.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        return link is not null && link.IsActive(now);
    }

    /// <summary>
    /// Redeems a link: validates it, sets the password, creates the account + default profile
    /// on first use, marks the link used. Does not create a session (the caller does).
    /// </summary>
    public async Task<ActivationResult> ActivateAsync(
        string rawToken, string displayName, string password, string requestLanguage, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = Tokens.Hash(rawToken);
        var link = await db.RegistrationLinks.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (link is null || !link.IsActive(now))
        {
            return ActivationResult.Fail(ActivationError.InvalidLink);
        }

        displayName = displayName?.Trim() ?? "";
        if (displayName.Length is < 1 or > 80)
        {
            return ActivationResult.Fail(ActivationError.InvalidName);
        }

        if (!PasswordRules.IsAcceptable(password))
        {
            return ActivationResult.Fail(ActivationError.WeakPassword);
        }

        var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == link.Email, ct);
        if (user is null)
        {
            user = new User { Email = link.Email, CreatedAt = now };
            db.Users.Add(user);
        }

        if (user.Profile is null)
        {
            user.Profile = new Profile
            {
                User = user,
                DisplayName = displayName,
                TimeZoneId = Opt.DefaultTimeZone,
                Language = SupportedLanguages.Match(requestLanguage) ?? SupportedLanguages.Default,
            };
        }
        else
        {
            user.Profile.DisplayName = displayName;
        }

        user.PasswordHash = hasher.Hash(password);
        link.UsedAt = now;
        await db.SaveChangesAsync(ct);

        return new ActivationResult(ActivationError.None, user.Id);
    }

    internal static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
