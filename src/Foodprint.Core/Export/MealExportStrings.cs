namespace Foodprint.Core.Export;

/// <summary>
/// Every localized string the Markdown render needs, supplied by the caller so
/// <see cref="Foodprint.Core"/> keeps no dependency on the web localization stack.
/// Unit tests construct this with fixed English text.
/// </summary>
public sealed record MealExportStrings
{
    // Header
    public required string Title { get; init; }
    public required string Intro { get; init; }
    public required string GeneratedLabel { get; init; }
    public required string TimeZoneLabel { get; init; }
    public required string LanguageLabel { get; init; }
    public required string RangeLabel { get; init; }
    public required string RangeOpenStart { get; init; }   // shown when there is no start date
    public required string TotalMealsLabel { get; init; }

    // Legend
    public required string LegendHeading { get; init; }
    public required string LegendPortionIntro { get; init; }
    public required string LegendGramsNote { get; init; }
    public required string LegendGroupsIntro { get; init; }

    // Analysis
    public required string AnalysisHeading { get; init; }
    public required string DaysWithEntriesLabel { get; init; }
    public required string PerDayHeading { get; init; }
    public required string ByGroupHeading { get; init; }
    public required string TagsHeading { get; init; }
    public required string StreakLabel { get; init; }
    public required string MissingDaysHeading { get; init; }

    /// <summary>Composite formatter: (count, firstDate, lastDate) -&gt; sentence, used when the span is too long to list.</summary>
    public required Func<int, string, string, string> MissingDaysSummary { get; init; }

    // Entries
    public required string EntriesHeading { get; init; }
    public required string NoEntries { get; init; }

    // Table columns
    public required string ColDate { get; init; }
    public required string ColTime { get; init; }
    public required string ColName { get; init; }
    public required string ColGroup { get; init; }
    public required string ColPortion { get; init; }
    public required string ColTags { get; init; }
    public required string ColNotes { get; init; }
    public required string ColCount { get; init; }

    public required string NoGroup { get; init; }

    // Dynamic label maps (fall back to the key itself when unknown)
    public required Func<string, string> SizeLabel { get; init; }
    public required Func<string, string> SizeLegend { get; init; }
    public required Func<string, string> GroupLabel { get; init; }
}
