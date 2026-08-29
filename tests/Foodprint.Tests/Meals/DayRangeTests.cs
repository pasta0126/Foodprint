using Foodprint.Core.Meals;

namespace Foodprint.Tests.Meals;

public class DayRangeTests
{
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    private static readonly TimeZoneInfo Auckland = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");

    [Fact]
    public void Normal_day_is_24_hours()
    {
        var range = DayRange.For(new DateOnly(2026, 1, 15), Madrid);
        Assert.Equal(TimeSpan.FromHours(24), range.EndUtc - range.StartUtc);
        Assert.Equal(new DateTime(2026, 1, 14, 23, 0, 0, DateTimeKind.Utc), range.StartUtc);
    }

    [Fact]
    public void Spring_forward_day_is_23_hours()
    {
        // Europe/Madrid: 2026-03-29, clocks jump 02:00 -> 03:00.
        var range = DayRange.For(new DateOnly(2026, 3, 29), Madrid);
        Assert.Equal(TimeSpan.FromHours(23), range.EndUtc - range.StartUtc);
    }

    [Fact]
    public void Fall_back_day_is_25_hours()
    {
        // Europe/Madrid: 2026-10-25, clocks fall 03:00 -> 02:00.
        var range = DayRange.For(new DateOnly(2026, 10, 25), Madrid);
        Assert.Equal(TimeSpan.FromHours(25), range.EndUtc - range.StartUtc);
    }

    [Fact]
    public void Instant_near_midnight_buckets_into_local_day()
    {
        // 2026-07-01 01:00 in Auckland (NZST, UTC+12) is 2026-06-30 13:00 UTC.
        var localEarly = new DateTime(2026, 7, 1, 1, 0, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(localEarly, Auckland);

        Assert.Equal(new DateOnly(2026, 6, 30), DateOnly.FromDateTime(utc));             // UTC day
        Assert.Equal(new DateOnly(2026, 7, 1), DayRange.LocalDate(utc, Auckland));       // local day
    }

    [Fact]
    public void Range_contains_matches_local_date()
    {
        var date = new DateOnly(2026, 3, 29);
        var range = DayRange.For(date, Madrid);
        var noonUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified), Madrid);

        Assert.True(range.Contains(noonUtc));
        Assert.False(range.Contains(range.EndUtc));
    }
}
