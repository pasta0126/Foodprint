using Foodprint.Core.Export;
using Foodprint.Core.Meals;

namespace Foodprint.Tests.Export;

public class MealExportMarkdownTests
{
    private static readonly MealExportStrings S = ExportStringsFixture.English;

    private static MealExportModel Model(
        IReadOnlyList<ExportEntry>? entries = null,
        IReadOnlyList<DayCount>? perDay = null,
        IReadOnlyList<GroupCount>? byGroup = null,
        IReadOnlyList<TagCount>? tags = null,
        IReadOnlyList<DateOnly>? missing = null,
        int total = 0,
        int days = 0,
        int streak = 0) => new(
            From: new DateOnly(2026, 8, 1),
            To: new DateOnly(2026, 8, 7),
            ReportedFrom: new DateOnly(2026, 8, 1),
            GeneratedLocalDate: new DateOnly(2026, 8, 30),
            GeneratedLocalTime: new TimeOnly(14, 5),
            TimeZoneId: "Europe/Madrid",
            LanguageLabel: "English",
            TotalMeals: total,
            DaysWithEntries: days,
            Streak: streak,
            PerDay: perDay ?? [],
            ByGroup: byGroup ?? [],
            Tags: tags ?? [],
            MissingDays: missing,
            MissingDaysTooMany: null,
            Entries: entries ?? []);

    [Fact]
    public void Header_and_legend_are_present()
    {
        var md = MealExportMarkdown.Render(Model(), S);

        Assert.Contains("# Foodprint meal log", md);
        Assert.Contains("Meal diary exported for AI analysis.", md);
        Assert.Contains("Time zone: Europe/Madrid", md);
        Assert.Contains("## Legend", md);
        Assert.Contains("**Small** — legend for small", md);
        Assert.Contains("**Very large** — legend for very-large", md);
        Assert.Contains("`breakfast` — Breakfast", md);
        Assert.Contains("A portion may instead be recorded in grams.", md);
    }

    [Fact]
    public void Aggregate_sections_render_as_tables()
    {
        var md = MealExportMarkdown.Render(Model(
            total: 10, days: 4, streak: 3,
            perDay: [new(new DateOnly(2026, 8, 1), 3), new(new DateOnly(2026, 8, 3), 7)],
            byGroup: [new("lunch", 6), new(null, 4)],
            tags: [new("lunch", 6), new("home", 2)],
            missing: [new DateOnly(2026, 8, 2), new DateOnly(2026, 8, 4)]), S);

        Assert.Contains("Total meals: 10", md);
        Assert.Contains("Days with entries: 4", md);
        Assert.Contains("Current streak (days, as of today): 3", md);
        Assert.Contains("| Lunch | 6 |", md);
        Assert.Contains("| (no group) | 4 |", md);
        Assert.Contains("| #lunch | 6 |", md);
        Assert.Contains("## Days with no entry", md);
        Assert.Contains("- 2026-08-02", md);
        Assert.Contains("- 2026-08-04", md);
    }

    [Fact]
    public void Entry_rows_map_fields_and_escape_pipes()
    {
        var entries = new List<ExportEntry>
        {
            new(new DateOnly(2026, 8, 1), new TimeOnly(9, 30), "Pa amb | tomàquet",
                "breakfast", "large", null, ["home"], "note with\nnewline"),
            new(new DateOnly(2026, 8, 1), new TimeOnly(13, 0), "Yogurt",
                null, null, 250, [], null),
        };

        var md = MealExportMarkdown.Render(Model(entries: entries, total: 2), S);
        var rows = md.Split('\n').Where(l => l.StartsWith("| 2026-08-01")).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Contains("Pa amb \\| tomàquet", rows[0]);
        Assert.Contains("| Large |", rows[0]);
        Assert.Contains("| Breakfast |", rows[0]);
        Assert.Contains("note with newline", rows[0]);   // CR/LF collapsed
        Assert.DoesNotContain("\n", rows[0].TrimEnd());
        Assert.Contains("| 250 g |", rows[1]);
        // every data row keeps the 7-column shape: 8 unescaped pipes
        Assert.All(rows, r => Assert.Equal(8, UnescapedPipes(r)));
    }

    private static int UnescapedPipes(string row)
    {
        var n = 0;
        for (var i = 0; i < row.Length; i++)
        {
            if (row[i] == '|' && (i == 0 || row[i - 1] != '\\'))
            {
                n++;
            }
        }

        return n;
    }

    [Fact]
    public void Empty_range_still_has_header_and_a_no_entries_note()
    {
        var md = MealExportMarkdown.Render(Model(), S);

        Assert.Contains("# Foodprint meal log", md);
        Assert.Contains("## Legend", md);
        Assert.Contains("Total meals: 0", md);
        Assert.Contains("_No entries in this range._", md);
        Assert.DoesNotContain("| Date | Time | Name |", md);
    }
}
