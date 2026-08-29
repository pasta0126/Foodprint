using Foodprint.Core.Auth;
using Foodprint.Core.Domain;
using Foodprint.Core.Meals;
using Foodprint.Tests.Infrastructure;

namespace Foodprint.Tests.Meals;

public class SummaryServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly IPasswordHasher _hasher = new Argon2PasswordHasher();
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    private static readonly DateOnly Today = new(2026, 8, 26); // a Wednesday

    private SummaryService Summary() => new(_db.NewContext());

    private async Task<Guid> NewUser()
    {
        var reg = new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options());
        var token = await reg.CreateLinkAsync("s@example.com", byAdmin: true);
        await new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options())
            .ActivateAsync(token!, "S", "correcthorse1", "es");
        await using var db = _db.NewContext();
        return db.Users.Single().Id;
    }

    private async Task Add(Guid userId, DateOnly localDate, params string[] tags)
    {
        var utc = DayRange.For(localDate, Madrid).StartUtc.AddHours(12);
        await using var db = _db.NewContext();
        var entry = new MealEntry { UserId = userId, Name = "meal", EatenAt = utc };
        foreach (var t in tags)
        {
            var tag = db.Tags.SingleOrDefault(x => x.UserId == userId && x.Name == t)
                ?? new Tag { UserId = userId, Name = t };
            entry.EntryTags.Add(new MealEntryTag { Tag = tag });
        }

        db.MealEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Window_is_seven_days_ending_today()
    {
        var user = await NewUser();
        var summary = await Summary().GetAsync(user, Madrid, Today);

        Assert.Equal(new DateOnly(2026, 8, 20), summary.From); // previous Thursday
        Assert.Equal(Today, summary.To);
        Assert.Equal(7, summary.PerDay.Count);
    }

    [Fact]
    public async Task Per_day_counts_include_zero_days()
    {
        var user = await NewUser();
        await Add(user, Today);
        await Add(user, Today);
        await Add(user, Today.AddDays(-3));

        var summary = await Summary().GetAsync(user, Madrid, Today);

        Assert.Equal(2, summary.PerDay.Single(d => d.Date == Today).Count);
        Assert.Equal(1, summary.PerDay.Single(d => d.Date == Today.AddDays(-3)).Count);
        Assert.Equal(5, summary.PerDay.Count(d => d.Count == 0));
    }

    [Fact]
    public async Task Entries_outside_the_window_are_excluded()
    {
        var user = await NewUser();
        await Add(user, Today.AddDays(-7)); // one day before the window
        var summary = await Summary().GetAsync(user, Madrid, Today);
        Assert.All(summary.PerDay, d => Assert.Equal(0, d.Count));
    }

    [Fact]
    public async Task Top_tags_rank_by_count_then_name()
    {
        var user = await NewUser();
        // lunch x6, home x6, snack x3, work x1
        for (var i = 0; i < 6; i++) await Add(user, Today, "lunch", "home");
        for (var i = 0; i < 3; i++) await Add(user, Today.AddDays(-1), "snack");
        await Add(user, Today.AddDays(-2), "work");

        var tags = (await Summary().GetAsync(user, Madrid, Today)).TopTags;

        Assert.Equal(
            new[] { ("home", 6), ("lunch", 6), ("snack", 3), ("work", 1) },
            tags.Select(t => (t.Tag, t.Count)));
    }

    [Fact]
    public async Task Top_tags_capped_at_five()
    {
        var user = await NewUser();
        foreach (var t in new[] { "a", "b", "c", "d", "e", "f" })
        {
            await Add(user, Today, t);
        }

        Assert.Equal(5, (await Summary().GetAsync(user, Madrid, Today)).TopTags.Count);
    }

    [Fact]
    public async Task Streak_counts_consecutive_days_ending_today()
    {
        var user = await NewUser();
        await Add(user, Today);
        await Add(user, Today.AddDays(-1));
        await Add(user, Today.AddDays(-2));
        // gap at -3
        await Add(user, Today.AddDays(-4));

        Assert.Equal(3, (await Summary().GetAsync(user, Madrid, Today)).Streak);
    }

    [Fact]
    public async Task Streak_is_zero_when_nothing_logged_today()
    {
        var user = await NewUser();
        await Add(user, Today.AddDays(-1));
        await Add(user, Today.AddDays(-2));

        Assert.Equal(0, (await Summary().GetAsync(user, Madrid, Today)).Streak);
    }

    [Fact]
    public async Task Streak_can_extend_past_the_seven_day_window()
    {
        var user = await NewUser();
        for (var i = 0; i < 12; i++)
        {
            await Add(user, Today.AddDays(-i));
        }

        Assert.Equal(12, (await Summary().GetAsync(user, Madrid, Today)).Streak);
    }

    public void Dispose() => _db.Dispose();
}
