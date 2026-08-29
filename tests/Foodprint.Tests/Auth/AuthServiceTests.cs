using Foodprint.Core.Auth;
using Foodprint.Tests.Infrastructure;

namespace Foodprint.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly IPasswordHasher _hasher = new Argon2PasswordHasher();

    private RegistrationService Registration() =>
        new(_db.NewContext(), _hasher, _db.Clock, _db.Options());

    private AuthService Auth() => new(_db.NewContext(), _hasher, _db.Clock);

    private async Task<string> ActivatedUser(string email, string password)
    {
        var token = await Registration().CreateLinkAsync(email, byAdmin: true);
        var result = await Registration().ActivateAsync(token!, "Alex", password, "es");
        Assert.True(result.Ok);
        return email;
    }

    [Fact]
    public async Task Activation_first_use_creates_account_and_profile()
    {
        var token = await Registration().CreateLinkAsync("alex@example.com", byAdmin: false);
        var result = await Registration().ActivateAsync(token!, "Alex", "correcthorse1", "ca");

        Assert.True(result.Ok);
        await using var db = _db.NewContext();
        var user = db.Users.Single();
        Assert.Equal("alex@example.com", user.Email);
        Assert.NotNull(user.PasswordHash);
    }

    [Fact]
    public async Task Activation_rejects_weak_password_and_saves_nothing()
    {
        var token = await Registration().CreateLinkAsync("weak@example.com", byAdmin: false);
        var result = await Registration().ActivateAsync(token!, "Weak", "short", "es");

        Assert.Equal(ActivationError.WeakPassword, result.Error);
        await using var db = _db.NewContext();
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Activation_link_is_single_use()
    {
        var token = await Registration().CreateLinkAsync("once@example.com", byAdmin: true);
        Assert.True((await Registration().ActivateAsync(token!, "Once", "correcthorse1", "es")).Ok);

        var second = await Registration().ActivateAsync(token!, "Once", "correcthorse2", "es");
        Assert.Equal(ActivationError.InvalidLink, second.Error);
    }

    [Fact]
    public async Task Activation_link_expires()
    {
        var token = await Registration().CreateLinkAsync("late@example.com", byAdmin: true);
        _db.Clock.Advance(TimeSpan.FromDays(31));

        var result = await Registration().ActivateAsync(token!, "Late", "correcthorse1", "es");
        Assert.Equal(ActivationError.InvalidLink, result.Error);
    }

    [Fact]
    public async Task Revoked_link_cannot_be_redeemed()
    {
        var reg = Registration();
        var token = await reg.CreateLinkAsync("revoke@example.com", byAdmin: true);
        var id = (await reg.ListAsync())[0].Id;
        Assert.True(await Registration().RevokeAsync(id));

        var result = await Registration().ActivateAsync(token!, "Rev", "correcthorse1", "es");
        Assert.Equal(ActivationError.InvalidLink, result.Error);
    }

    [Fact]
    public async Task Self_register_blocked_when_email_already_activated()
    {
        await ActivatedUser("taken@example.com", "correcthorse1");

        var again = await Registration().CreateLinkAsync("taken@example.com", byAdmin: false);
        Assert.Null(again);

        var adminReset = await Registration().CreateLinkAsync("taken@example.com", byAdmin: true);
        Assert.NotNull(adminReset);
    }

    [Fact]
    public async Task SignIn_succeeds_with_correct_password()
    {
        await ActivatedUser("in@example.com", "correcthorse1");
        var result = await Auth().SignInAsync("in@example.com", "correcthorse1");

        Assert.True(result.Ok);
        Assert.NotNull(result.SessionToken);
    }

    [Theory]
    [InlineData("in@example.com", "wrongpassword")]
    [InlineData("unknown@example.com", "correcthorse1")]
    public async Task SignIn_fails_generically(string email, string password)
    {
        await ActivatedUser("in@example.com", "correcthorse1");
        var result = await Auth().SignInAsync(email, password);
        Assert.Equal(SignInError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Disabled_user_cannot_sign_in_and_sessions_are_cleared()
    {
        await ActivatedUser("gone@example.com", "correcthorse1");
        var session = (await Auth().SignInAsync("gone@example.com", "correcthorse1")).SessionToken!;

        Assert.True(await Auth().SetDisabledAsync("gone@example.com", true));

        Assert.Null(await Auth().ResolveSessionAsync(session));
        Assert.Equal(SignInError.InvalidCredentials, (await Auth().SignInAsync("gone@example.com", "correcthorse1")).Error);
    }

    [Fact]
    public async Task Session_expires_after_30_idle_days()
    {
        await ActivatedUser("idle@example.com", "correcthorse1");
        var session = (await Auth().SignInAsync("idle@example.com", "correcthorse1")).SessionToken!;

        _db.Clock.Advance(TimeSpan.FromDays(31));
        Assert.Null(await Auth().ResolveSessionAsync(session));
    }

    [Fact]
    public async Task Password_change_invalidates_other_sessions_but_keeps_current()
    {
        var email = await ActivatedUser("chg@example.com", "correcthorse1");
        await using var db = _db.NewContext();
        var userId = db.Users.Single(u => u.Email == email).Id;

        var stale = await Auth().CreateSessionAsync(userId);
        var current = await Auth().CreateSessionAsync(userId);

        var err = await Auth().ChangePasswordAsync(userId, "correcthorse1", "brandnewpass9", current);
        Assert.Equal(PasswordChangeError.None, err);

        Assert.Null(await Auth().ResolveSessionAsync(stale));
        Assert.NotNull(await Auth().ResolveSessionAsync(current));
    }

    [Fact]
    public async Task Password_change_rejects_wrong_current_and_weak_new()
    {
        var email = await ActivatedUser("v@example.com", "correcthorse1");
        await using var db = _db.NewContext();
        var userId = db.Users.Single(u => u.Email == email).Id;

        Assert.Equal(PasswordChangeError.WrongCurrent, await Auth().ChangePasswordAsync(userId, "nope", "brandnewpass9", null));
        Assert.Equal(PasswordChangeError.WeakPassword, await Auth().ChangePasswordAsync(userId, "correcthorse1", "short", null));
    }

    public void Dispose() => _db.Dispose();
}
