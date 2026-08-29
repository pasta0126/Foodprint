using Foodprint.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Meals;

public sealed record DiaryDay(DateOnly Date, IReadOnlyList<MealEntryView> Entries);

public sealed record HistoryDay(DateOnly Date, int Count, IReadOnlyList<string> NamePreview);

public sealed record HistoryPage(IReadOnlyList<HistoryDay> Days, int Page, bool HasMore);

/// <summary>Day view and history list over a user's meal entries, bucketed by their profile time zone.</summary>
public sealed class DiaryService(AppDbContext db)
{
    public const int HistoryPageSize = 20;
    private const int PreviewCount = 3;

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

    /// <summary>Reverse-chronological days that have at least one entry, 20 per page.</summary>
    public async Task<HistoryPage> GetHistoryAsync(Guid userId, TimeZoneInfo zone, int page, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);

        // Personal-scale data: pull the (time, name) pairs and bucket by local date in memory.
        var rows = await db.MealEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EatenAt)
            .Select(e => new { e.EatenAt, e.Name })
            .ToListAsync(ct);

        var byDay = rows
            .GroupBy(r => DayRange.LocalDate(r.EatenAt, zone))
            .OrderByDescending(g => g.Key)
            .Select(g => new HistoryDay(
                g.Key,
                g.Count(),
                g.Take(PreviewCount).Select(r => r.Name).ToList()))
            .ToList();

        var slice = byDay.Skip((page - 1) * HistoryPageSize).Take(HistoryPageSize).ToList();
        var hasMore = byDay.Count > page * HistoryPageSize;
        return new HistoryPage(slice, page, hasMore);
    }
}
