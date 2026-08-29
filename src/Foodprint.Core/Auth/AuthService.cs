using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Auth;

public sealed record AuthenticatedUser(Guid Id, string Email, string DisplayName, bool IsAdmin, string Language, string TimeZoneId);

/// <summary>Password sign-in, session lifecycle, password changes, account enable/disable.</summary>
public sealed class AuthService(AppDbContext db, IPasswordHasher hasher, TimeProvider clock)
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan RenewThreshold = TimeSpan.FromHours(24);

    public async Task<SignInResult> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        email = RegistrationService.Normalize(email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same generic outcome whether the email is unknown, the password is wrong, the
        // account was never activated, or it is disabled.
        if (user is null || user.PasswordHash is null || user.IsDisabled
            || !hasher.Verify(password, user.PasswordHash))
        {
            return SignInResult.Fail(SignInError.InvalidCredentials);
        }

        var token = await CreateSessionAsync(user.Id, ct);
        return new SignInResult(SignInError.None, user.Id, token);
    }

    public async Task<string> CreateSessionAsync(Guid userId, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var raw = Tokens.Generate();
        db.Sessions.Add(new Session
        {
            UserId = userId,
            TokenHash = Tokens.Hash(raw),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now + SessionLifetime,
        });
        await db.SaveChangesAsync(ct);
        return raw;
    }

    /// <summary>Resolves a cookie token to the current user, applying rolling renewal. Null when invalid.</summary>
    public async Task<AuthenticatedUser?> ResolveSessionAsync(string rawToken, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = Tokens.Hash(rawToken);
        var session = await db.Sessions
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(s => s.TokenHash == hash, ct);

        if (session is null || session.ExpiresAt <= now || session.User.IsDisabled
            || session.User.PasswordHash is null || session.User.Profile is null)
        {
            return null;
        }

        if (now - session.LastSeenAt >= RenewThreshold)
        {
            session.LastSeenAt = now;
            session.ExpiresAt = now + SessionLifetime;
            await db.SaveChangesAsync(ct);
        }

        var u = session.User;
        return new AuthenticatedUser(u.Id, u.Email, u.Profile.DisplayName, u.IsAdmin, u.Profile.Language, u.Profile.TimeZoneId);
    }

    public async Task SignOutAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = Tokens.Hash(rawToken);
        await db.Sessions.Where(s => s.TokenHash == hash).ExecuteDeleteAsync(ct);
    }

    public async Task<PasswordChangeError> ChangePasswordAsync(
        Guid userId, string current, string next, string? keepSessionToken, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.PasswordHash is null || !hasher.Verify(current, user.PasswordHash))
        {
            return PasswordChangeError.WrongCurrent;
        }

        if (!PasswordRules.IsAcceptable(next))
        {
            return PasswordChangeError.WeakPassword;
        }

        user.PasswordHash = hasher.Hash(next);

        var keepHash = keepSessionToken is null ? null : Tokens.Hash(keepSessionToken);
        await db.Sessions.Where(s => s.UserId == userId && (keepHash == null || s.TokenHash != keepHash))
            .ExecuteDeleteAsync(ct);

        await db.SaveChangesAsync(ct);
        return PasswordChangeError.None;
    }

    public async Task<bool> SetDisabledAsync(string email, bool disabled, CancellationToken ct = default)
    {
        email = RegistrationService.Normalize(email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return false;
        }

        user.DisabledAt = disabled ? clock.GetUtcNow().UtcDateTime : null;
        if (disabled)
        {
            await db.Sessions.Where(s => s.UserId == user.Id).ExecuteDeleteAsync(ct);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
