using Foodprint.Core.Export;
using Foodprint.Web.Resources;
using Microsoft.Extensions.Localization;

namespace Foodprint.Web.Export;

/// <summary>Builds the Core render's string bag from the active-culture resources.</summary>
public static class ExportStrings
{
    public static MealExportStrings From(IStringLocalizer<SharedResource> l) => new()
    {
        Title = l["Export.Md.Title"],
        Intro = l["Export.Md.Intro"],
        GeneratedLabel = l["Export.Md.Generated"],
        TimeZoneLabel = l["Export.Md.TimeZone"],
        LanguageLabel = l["Export.Md.Language"],
        RangeLabel = l["Export.Md.Range"],
        RangeOpenStart = l["Export.Md.RangeOpenStart"],
        TotalMealsLabel = l["Export.Md.TotalMeals"],

        LegendHeading = l["Export.Md.LegendHeading"],
        LegendPortionIntro = l["Export.Md.LegendPortionIntro"],
        LegendGramsNote = l["Export.Md.LegendGramsNote"],
        LegendGroupsIntro = l["Export.Md.LegendGroupsIntro"],

        AnalysisHeading = l["Export.Md.AnalysisHeading"],
        DaysWithEntriesLabel = l["Export.Md.DaysWithEntries"],
        PerDayHeading = l["Export.Md.PerDayHeading"],
        ByGroupHeading = l["Export.Md.ByGroupHeading"],
        TagsHeading = l["Export.Md.TagsHeading"],
        StreakLabel = l["Export.Md.StreakLabel"],
        MissingDaysHeading = l["Export.Md.MissingDaysHeading"],
        MissingDaysSummary = (n, a, b) => string.Format(l["Export.Md.MissingDays.Summary"].Value, n, a, b),

        EntriesHeading = l["Export.Md.EntriesHeading"],
        NoEntries = l["Export.Md.NoEntries"],

        ColDate = l["Export.Md.Col.Date"],
        ColTime = l["Export.Md.Col.Time"],
        ColName = l["Export.Md.Col.Name"],
        ColGroup = l["Export.Md.Col.Group"],
        ColPortion = l["Export.Md.Col.Portion"],
        ColTags = l["Export.Md.Col.Tags"],
        ColNotes = l["Export.Md.Col.Notes"],
        ColCount = l["Export.Md.Col.Count"],
        NoGroup = l["Export.Md.NoGroup"],

        SizeLabel = k => l[$"Meal.Size.{k}"],
        SizeLegend = k => l[$"Meal.Size.{k}.Desc"],
        GroupLabel = k => l[$"MealGroup.{k}"],
    };
}
