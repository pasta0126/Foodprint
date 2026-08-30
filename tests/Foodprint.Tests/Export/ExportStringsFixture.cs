using Foodprint.Core.Export;

namespace Foodprint.Tests.Export;

/// <summary>Fixed English strings for export tests — mirrors what the web layer builds from resx.</summary>
internal static class ExportStringsFixture
{
    public static MealExportStrings English { get; } = new()
    {
        Title = "Foodprint meal log",
        Intro = "Meal diary exported for AI analysis.",
        GeneratedLabel = "Generated",
        TimeZoneLabel = "Time zone",
        LanguageLabel = "Language",
        RangeLabel = "Range",
        RangeOpenStart = "first entry",
        TotalMealsLabel = "Total meals",
        LegendHeading = "Legend",
        LegendPortionIntro = "Portion sizes, against a standard flat plate:",
        LegendGramsNote = "A portion may instead be recorded in grams.",
        LegendGroupsIntro = "Meal groups:",
        AnalysisHeading = "Analysis",
        DaysWithEntriesLabel = "Days with entries",
        PerDayHeading = "Meals per day",
        ByGroupHeading = "By meal group",
        TagsHeading = "Tags",
        StreakLabel = "Current streak (days, as of today)",
        MissingDaysHeading = "Days with no entry",
        MissingDaysSummary = (n, a, b) => $"{n} days with no entry, between {a} and {b}.",
        EntriesHeading = "Entries",
        NoEntries = "_No entries in this range._",
        ColDate = "Date",
        ColTime = "Time",
        ColName = "Name",
        ColGroup = "Group",
        ColPortion = "Portion",
        ColTags = "Tags",
        ColNotes = "Notes",
        ColCount = "Count",
        NoGroup = "(no group)",
        SizeLabel = k => k switch
        {
            "small" => "Small", "medium" => "Medium", "large" => "Large", "very-large" => "Very large", _ => k,
        },
        SizeLegend = k => $"legend for {k}",
        GroupLabel = k => k switch
        {
            "breakfast" => "Breakfast", "lunch" => "Lunch", "dinner" => "Dinner",
            "snack" => "Snack", "other" => "Other", _ => k,
        },
    };
}
