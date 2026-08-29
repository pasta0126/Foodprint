using Foodprint.Core.Auth;
using Foodprint.Core.Profiles;
using Foodprint.Tests.Infrastructure;

namespace Foodprint.Tests.Profiles;

public class ProfileServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly IPasswordHasher _hasher = new Argon2PasswordHasher();

    private ProfileService Profiles() => new(_db.NewContext());

    private async Task<Guid> NewUser(string email = "u@example.com", string lang = "es")
    {
        var reg = new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options());
        var token = await reg.CreateLinkAsync(email, byAdmin: true);
        await new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options())
            .ActivateAsync(token!, "Alex", "correcthorse1", lang);
        await using var db = _db.NewContext();
        return db.Users.Single(u => u.Email == email).Id;
    }

    [Fact]
    public async Task Defaults_on_creation_come_from_activation()
    {
        var id = await NewUser(lang: "ca");
        var view = await Profiles().GetAsync(id);

        Assert.NotNull(view);
        Assert.Equal("Alex", view!.DisplayName);
        Assert.Equal("Europe/Madrid", view.TimeZoneId);
        Assert.Equal("ca", view.Language);
    }

    [Fact]
    public async Task Unknown_request_language_falls_back_to_spanish()
    {
        var id = await NewUser(email: "fr@example.com", lang: "fr");
        Assert.Equal("es", (await Profiles().GetAsync(id))!.Language);
    }

    [Fact]
    public async Task Update_persists_valid_values()
    {
        var id = await NewUser();
        var error = await Profiles().UpdateAsync(id, "Alexandra", "America/New_York", "en");

        Assert.Equal(ProfileError.None, error);
        var view = await Profiles().GetAsync(id);
        Assert.Equal("America/New_York", view!.TimeZoneId);
        Assert.Equal("en", view.Language);
        Assert.Equal("Alexandra", view.DisplayName);
    }

    [Fact]
    public async Task Update_rejects_unknown_time_zone_and_keeps_stored_value()
    {
        var id = await NewUser();
        var error = await Profiles().UpdateAsync(id, "Alex", "Mars/Olympus", "es");

        Assert.Equal(ProfileError.InvalidTimeZone, error);
        Assert.Equal("Europe/Madrid", (await Profiles().GetAsync(id))!.TimeZoneId);
    }

    [Fact]
    public async Task Update_rejects_empty_name_and_unsupported_language()
    {
        var id = await NewUser();
        Assert.Equal(ProfileError.InvalidName, await Profiles().UpdateAsync(id, "   ", "Europe/Madrid", "es"));
        Assert.Equal(ProfileError.InvalidLanguage, await Profiles().UpdateAsync(id, "Alex", "Europe/Madrid", "de"));
    }

    [Fact]
    public async Task SetLanguage_takes_effect()
    {
        var id = await NewUser();
        Assert.Equal(ProfileError.None, await Profiles().SetLanguageAsync(id, "ca"));
        Assert.Equal("ca", (await Profiles().GetAsync(id))!.Language);
    }

    [Fact]
    public void ResolveZone_falls_back_to_utc_for_unknown()
    {
        Assert.Equal(TimeZoneInfo.Utc, ProfileService.ResolveZone("Nope/Nowhere"));
        Assert.Equal("America/New_York", ProfileService.ResolveZone("America/New_York").Id);
    }

    public void Dispose() => _db.Dispose();
}
