using System.Security.Claims;
using System.Xml.Linq;
using Foodprint.Core.Localization;
using Foodprint.Web.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Foodprint.Tests.Localization;

public class LanguageResolutionTests
{
    private static HttpContext Context(string? acceptLanguage = null, string? profileLang = null)
    {
        var ctx = new DefaultHttpContext();
        if (acceptLanguage is not null)
        {
            ctx.Request.Headers.AcceptLanguage = acceptLanguage;
        }

        if (profileLang is not null)
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(FoodprintRequestLocalization.LanguageClaim, profileLang)], "test"));
        }

        return ctx;
    }

    [Fact]
    public async Task Profile_language_wins_over_accept_language()
    {
        var provider = new ProfileClaimCultureProvider();
        var result = await provider.DetermineProviderCultureResult(Context(acceptLanguage: "ca", profileLang: "en"));

        Assert.Equal("en", result!.UICultures[0].Value);
    }

    [Fact]
    public async Task Accept_language_used_when_no_profile()
    {
        var options = FoodprintRequestLocalization.Build();
        var accept = options.RequestCultureProviders.OfType<AcceptLanguageHeaderRequestCultureProvider>().Single();

        var result = await accept.DetermineProviderCultureResult(Context(acceptLanguage: "ca"));

        Assert.Equal("ca", result!.UICultures[0].Value);
    }

    [Fact]
    public void Default_request_culture_is_spanish()
    {
        var options = FoodprintRequestLocalization.Build();
        Assert.Equal(SupportedLanguages.Default, options.DefaultRequestCulture.UICulture.Name);
        Assert.Equal("es", SupportedLanguages.Default);
    }

    [Fact]
    public void Provider_order_is_profile_then_cookie_then_accept_language()
    {
        var providers = FoodprintRequestLocalization.Build().RequestCultureProviders;

        Assert.IsType<ProfileClaimCultureProvider>(providers[0]);
        Assert.IsType<CookieRequestCultureProvider>(providers[1]);
        Assert.IsType<AcceptLanguageHeaderRequestCultureProvider>(providers[2]);
    }
}

public class ResourceCompletenessTests
{
    private static readonly string ResourceDir = LocateResources();

    public static TheoryData<string> Cultures() => new() { "ca", "en" };

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Every_neutral_key_exists_in_each_culture(string culture)
    {
        var neutral = Keys("SharedResource.resx");
        var localized = Keys($"SharedResource.{culture}.resx");

        var missing = neutral.Except(localized).ToList();
        var extra = localized.Except(neutral).ToList();

        Assert.True(missing.Count == 0, $"{culture} is missing: {string.Join(", ", missing)}");
        Assert.True(extra.Count == 0, $"{culture} has keys not in the neutral set: {string.Join(", ", extra)}");
    }

    [Fact]
    public void Meal_group_catalog_keys_are_all_translated()
    {
        var neutral = Keys("SharedResource.resx");
        foreach (var key in MealGroupKeysSeed())
        {
            Assert.Contains($"MealGroup.{key}", neutral);
        }
    }

    private static IEnumerable<string> MealGroupKeysSeed() =>
        Foodprint.Core.Domain.MealGroupKeys.Seed;

    private static HashSet<string> Keys(string file)
    {
        var doc = XDocument.Load(Path.Combine(ResourceDir, file));
        return doc.Root!.Elements("data").Select(e => e.Attribute("name")!.Value).ToHashSet();
    }

    private static string LocateResources()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src", "Foodprint.Web", "Resources")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return Path.Combine(dir!, "src", "Foodprint.Web", "Resources");
    }
}
