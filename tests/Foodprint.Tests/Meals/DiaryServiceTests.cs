using Foodprint.Core.Auth;
using Foodprint.Core.Domain;
using Foodprint.Core.Meals;
using Foodprint.Tests.Infrastructure;

namespace Foodprint.Tests.Meals;

public class DiaryServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly IPasswordHasher _hasher = new Argon2PasswordHasher();
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    private DiaryService Diary() => new(_db.NewContext());

    private async Task<Guid> NewUser()
    {
        var reg = new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options());
        var token = await reg.CreateLinkAsync("d@example.com", byAdmin: true);
        await new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options())
            .ActivateAsync(token!, "D", "correcthorse1", "es");
        await using var db = _db.NewContext();
        return db.Users.Single().Id;
    }

    private async Task Add(Guid userId, DateTime eatenAtUtc, string name)
    {
        await using var db = _db.NewContext();
        db.MealEntries.Add(new MealEntry { UserId = userId, Name = name, EatenAt = eatenAtUtc });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Day_view_returns_local_day_entries_in_time_order()
    {
        var user = await NewUser();
        // Local 2026-03-15 in Madrid == UTC 2026-03-14T23:00 .. 2026-03-15T23:00
        await Add(user, new DateTime(2026, 3, 14, 23, 30, 0, DateTimeKind.Utc), "late night (00:30 local)");
        await Add(user, new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc), "breakfast (09:00 local)");
        await Add(user, new DateTime(2026, 3, 15, 23, 30, 0, DateTimeKind.Utc), "next local day");

        var day = await Diary().GetDayAsync(user, new DateOnly(2026, 3, 15), Madrid);

        Assert.Equal(2, day.Entries.Count);
        Assert.Equal("late night (00:30 local)", day.Entries[0].Name);
        Assert.Equal("breakfast (09:00 local)", day.Entries[1].Name);
    }

    [Fact]
    public async Task Same_instants_bucket_differently_by_zone()
    {
        var user = await NewUser();
        var utc = new DateTime(2026, 3, 15, 23, 30, 0, DateTimeKind.Utc); // 00:30 on the 16th in Madrid
        await Add(user, utc, "x");

        var madrid = await Diary().GetDayAsync(user, new DateOnly(2026, 3, 16), Madrid);
        var utcZone = await Diary().GetDayAsync(user, new DateOnly(2026, 3, 15), TimeZoneInfo.Utc);

        Assert.Single(madrid.Entries);
        Assert.Single(utcZone.Entries);
    }

    [Fact]
    public async Task Empty_day_returns_no_entries()
    {
        var user = await NewUser();
        var day = await Diary().GetDayAsync(user, new DateOnly(2026, 1, 1), Madrid);
        Assert.Empty(day.Entries);
    }

    [Fact]
    public async Task History_paginates_days_with_entries_newest_first()
    {
        var user = await NewUser();
        var baseUtc = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 45; i++)
        {
            await Add(user, baseUtc.AddDays(-i), $"day-{i}");
        }

        var page1 = await Diary().GetHistoryDaysAsync(user, Madrid, 1);
        Assert.Equal(20, page1.Days.Count);
        Assert.True(page1.HasMore);
        Assert.Equal(new DateOnly(2026, 6, 1), page1.Days[0].Date);
        Assert.True(page1.Days[0].Date > page1.Days[1].Date);

        var page3 = await Diary().GetHistoryDaysAsync(user, Madrid, 3);
        Assert.Equal(5, page3.Days.Count);
        Assert.False(page3.HasMore);
    }

    [Fact]
    public async Task History_day_carries_all_its_entries_in_time_order()
    {
        var user = await NewUser();
        var d = new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc);
        await Add(user, d.AddHours(6), "c");
        await Add(user, d, "a");
        await Add(user, d.AddHours(3), "b");

        var page = await Diary().GetHistoryDaysAsync(user, Madrid, 1);
        var day = Assert.Single(page.Days);
        Assert.Equal(["a", "b", "c"], day.Entries.Select(e => e.Name));
    }

    [Fact]
    public async Task History_empty_when_nothing_logged()
    {
        var user = await NewUser();
        var page = await Diary().GetHistoryDaysAsync(user, Madrid, 1);
        Assert.Empty(page.Days);
        Assert.False(page.HasMore);
    }

    public void Dispose() => _db.Dispose();
}
