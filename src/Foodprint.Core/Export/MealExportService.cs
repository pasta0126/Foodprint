using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Foodprint.Core.Meals;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Export;

/// <summary>A rendered export ready to hand to the browser as a download.</summary>
public sealed record MealExport(string FileName, string Markdown);

/// <summary>One entry as it appears in the export, already bucketed to the profile time zone.</summary>
public sealed record ExportEntry(
    DateOnly LocalDate,
    TimeOnly LocalTime,
    string Name,
    string? GroupKey,
    string? PortionSize,
    int? PortionGrams,
    IReadOnlyList<string> Tags,
    string? Notes);

/// <summary>Everything the Markdown render needs, all computed and ordered.</summary>
public sealed record MealExportModel(
    DateOnly? From,
    DateOnly To,
    DateOnly ReportedFrom,
    DateOnly GeneratedLocalDate,
    TimeOnly GeneratedLocalTime,
    string TimeZoneId,
    string LanguageLabel,
    int TotalMeals,
    int DaysWithEntries,
    int Streak,
    IReadOnlyList<DayCount> PerDay,
    IReadOnlyList<GroupCount> ByGroup,
    IReadOnlyList<TagCount> Tags,
    IReadOnlyList<DateOnly>? MissingDays,
    MissingDaysSummary? MissingDaysTooMany,
    IReadOnlyList<ExportEntry> Entries);

public sealed record GroupCount(string? GroupKey, int Count);

public sealed record MissingDaysSummary(int Count, DateOnly First, DateOnly Last);

/// <summary>Builds the Markdown meal-log export for one user and date range. Read-only, user-scoped.</summary>
public sealed class MealExportService(AppDbContext db, TimeProvider clock)
{
    /// <summary>Above this many days in the reported range, missing days are summarised rather than listed.</summary>
    public const int MissingDaysListCap = 92;

    public async Task<MealExport> BuildAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        TimeZoneInfo zone,
        string languageLabel,
        MealExportStrings strings,
        CancellationToken ct = default)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var today = DayRange.LocalDate(nowUtc, zone);

        var resolvedTo = to ?? today;
        var resolvedFrom = from;
        if (resolvedFrom is { } f && f > resolvedTo)
        {
            resolvedFrom = null;
            resolvedTo = today;
        }

        var startUtc = resolvedFrom is { } rf ? DayRange.For(rf, zone).StartUtc : DateTime.MinValue;
        var endUtc = DayRange.For(resolvedTo, zone).EndUtc;

        var rows = await db.MealEntries
            .Where(e => e.UserId == userId && e.EatenAt >= startUtc && e.EatenAt < endUtc)
            .Include(e => e.MealGroup)
            .Include(e => e.EntryTags).ThenInclude(t => t.Tag)
            .OrderBy(e => e.EatenAt)
            .ToListAsync(ct);

        var entries = rows
            .Select(e => new ExportEntry(
                DayRange.LocalDate(e.EatenAt, zone),
                TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(e.EatenAt, DateTimeKind.Utc), zone)),
                e.Name,
                e.MealGroup?.Key,
                e.PortionSize,
                e.PortionGrams,
                e.EntryTags.Select(t => t.Tag.Name).OrderBy(n => n, StringComparer.Ordinal).ToList(),
                string.IsNullOrWhiteSpace(e.Notes) ? null : e.Notes))
            .ToList();

        var streak = await ComputeStreakAsync(userId, zone, today, ct);
        var model = BuildModel(entries, rows, resolvedFrom, resolvedTo, today, nowUtc, zone, languageLabel, streak);

        var md = MealExportMarkdown.Render(model, strings);
        var fileName = $"foodprint-{(resolvedFrom is { } df ? df.ToString("yyyy-MM-dd") : "all")}-{resolvedTo:yyyy-MM-dd}.md";
        return new MealExport(fileName, md);
    }

    private async Task<int> ComputeStreakAsync(Guid userId, TimeZoneInfo zone, DateOnly today, CancellationToken ct)
    {
        var since = DayRange.For(today.AddDays(-MealStreak.LookbackDays), zone).StartUtc;
        var times = await db.MealEntries
            .Where(e => e.UserId == userId && e.EatenAt >= since)
            .Select(e => e.EatenAt)
            .ToListAsync(ct);

        return MealStreak.Current(times.Select(t => DayRange.LocalDate(t, zone)).ToHashSet(), today);
    }

    private static MealExportModel BuildModel(
        List<ExportEntry> entries,
        List<MealEntry> rows,
        DateOnly? from,
        DateOnly to,
        DateOnly today,
        DateTime nowUtc,
        TimeZoneInfo zone,
        string languageLabel,
        int streak)
    {
        var localGenerated = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), zone);

        var distinctDays = entries.Select(e => e.LocalDate).Distinct().OrderBy(d => d).ToList();
        var reportedFrom = from ?? (distinctDays.Count > 0 ? distinctDays[0] : to);

        var perDay = distinctDays
            .Select(d => new DayCount(d, entries.Count(e => e.LocalDate == d)))
            .ToList();

        var byGroup = rows
            .GroupBy(e => new { e.MealGroup?.Key, Sort = e.MealGroup?.SortOrder ?? int.MaxValue })
            .OrderBy(g => g.Key.Sort)
            .Select(g => new GroupCount(g.Key.Key, g.Count()))
            .ToList();

        var tags = entries
            .SelectMany(e => e.Tags)
            .GroupBy(t => t)
            .Select(g => new TagCount(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Tag, StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<DateOnly>? missingDays = null;
        MissingDaysSummary? missingTooMany = null;
        if (entries.Count > 0)
        {
            var have = distinctDays.ToHashSet();
            var span = new List<DateOnly>();
            for (var d = reportedFrom; d <= to; d = d.AddDays(1))
            {
                if (!have.Contains(d))
                {
                    span.Add(d);
                }
            }

            var rangeLength = to.DayNumber - reportedFrom.DayNumber + 1;
            if (span.Count == 0)
            {
                missingDays = [];
            }
            else if (rangeLength <= MissingDaysListCap)
            {
                missingDays = span;
            }
            else
            {
                missingTooMany = new MissingDaysSummary(span.Count, span[0], span[^1]);
            }
        }

        return new MealExportModel(
            from, to, reportedFrom,
            DateOnly.FromDateTime(localGenerated), TimeOnly.FromDateTime(localGenerated),
            zone.Id, languageLabel,
            entries.Count, distinctDays.Count, streak,
            perDay, byGroup, tags, missingDays, missingTooMany, entries);
    }
}
