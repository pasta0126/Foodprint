using Foodprint.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Meals;

public sealed record DiaryDay(DateOnly Date, IReadOnlyList<MealEntryView> Entries);

public sealed record HistoryDays(IReadOnlyList<DiaryDay> Days, int Page, bool HasMore);

/// <summary>Day view and history over a user's meal entries, bucketed by their profile time zone.</summary>
public sealed class DiaryService(AppDbContext db)
{
    public const int HistoryPageSize = 20;

    public async Task<DiaryDay> GetDayAsync(Guid userId, DateOnly date, TimeZoneInfo zone, CancellationToken ct = default)
    {
        var range = DayRange.For(date, zone);
        var entries = await db.MealEntries
            .Where(e => e.UserId == userId && e.EatenAt >= range.StartUtc && e.EatenAt < range.EndUtc)
            .Include(e => e.MealGroup)
            .Include(e => e.EntryTags).ThenInclude(t => t.Tag)
            .OrderBy(e => e.EatenAt)
            .ToListAsync(ct);

        return new DiaryDay(date, entries.Select(MealEntryService.ToView).ToList());
    }

    /// <summary>
    /// Reverse-chronological days that have at least one entry, 20 per page, each day
    /// carrying all its entries (time ascending). Bucketed by the user's profile time zone.
    /// </summary>
    public async Task<HistoryDays> GetHistoryDaysAsync(Guid userId, TimeZoneInfo zone, int page, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);

        // Personal-scale data: pull every entry once, bucket to local dates in memory.
        var entries = await db.MealEntries
            .Where(e => e.UserId == userId)
            .Include(e => e.MealGroup)
            .Include(e => e.EntryTags).ThenInclude(t => t.Tag)
            .OrderBy(e => e.EatenAt)
            .ToListAsync(ct);

        var byDay = entries
            .GroupBy(e => DayRange.LocalDate(e.EatenAt, zone))
            .OrderByDescending(g => g.Key)
            .Select(g => new DiaryDay(g.Key, g.Select(MealEntryService.ToView).ToList()))
            .ToList();

        var slice = byDay.Skip((page - 1) * HistoryPageSize).Take(HistoryPageSize).ToList();
        var hasMore = byDay.Count > page * HistoryPageSize;
        return new HistoryDays(slice, page, hasMore);
    }
}
