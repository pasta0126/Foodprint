using Foodprint.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Meals;

public sealed record DayCount(DateOnly Date, int Count);

public sealed record TagCount(string Tag, int Count);

public sealed record WeeklySummary(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DayCount> PerDay,
    IReadOnlyList<TagCount> TopTags,
    int Streak);

/// <summary>The last-seven-days habit view: entries per day, top tags, current logging streak.</summary>
public sealed class SummaryService(AppDbContext db)
{
    private const int WindowDays = 7;
    private const int TopTagCount = 5;

    public async Task<WeeklySummary> GetAsync(Guid userId, TimeZoneInfo zone, DateOnly today, CancellationToken ct = default)
    {
        var from = today.AddDays(-(WindowDays - 1));
        var windowStart = DayRange.For(from, zone).StartUtc;
        var windowEnd = DayRange.For(today, zone).EndUtc;

        var windowEntries = await db.MealEntries
            .Where(e => e.UserId == userId && e.EatenAt >= windowStart && e.EatenAt < windowEnd)
            .Select(e => new { e.EatenAt, Tags = e.EntryTags.Select(t => t.Tag.Name) })
            .ToListAsync(ct);

        var localDates = windowEntries
            .Select(e => DayRange.LocalDate(e.EatenAt, zone))
            .ToList();

        var perDay = Enumerable.Range(0, WindowDays)
            .Select(offset => from.AddDays(offset))
            .Select(date => new DayCount(date, localDates.Count(d => d == date)))
            .ToList();

        var topTags = windowEntries
            .SelectMany(e => e.Tags)
            .GroupBy(t => t)
            .Select(g => new TagCount(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Tag, StringComparer.Ordinal)
            .Take(TopTagCount)
            .ToList();

        var streak = await ComputeStreakAsync(userId, zone, today, ct);

        return new WeeklySummary(from, today, perDay, topTags, streak);
    }

    private async Task<int> ComputeStreakAsync(Guid userId, TimeZoneInfo zone, DateOnly today, CancellationToken ct)
    {
        var since = DayRange.For(today.AddDays(-MealStreak.LookbackDays), zone).StartUtc;
        var times = await db.MealEntries
            .Where(e => e.UserId == userId && e.EatenAt >= since)
            .Select(e => e.EatenAt)
            .ToListAsync(ct);

        var daysWithEntries = times.Select(t => DayRange.LocalDate(t, zone)).ToHashSet();
        return MealStreak.Current(daysWithEntries, today);
    }
}
