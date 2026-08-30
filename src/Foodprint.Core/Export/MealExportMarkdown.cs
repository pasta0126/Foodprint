using System.Globalization;
using System.Text;
using Foodprint.Core.Domain;

namespace Foodprint.Core.Export;

/// <summary>Renders a <see cref="MealExportModel"/> as the export Markdown. Pure; unit-tested directly.</summary>
public static class MealExportMarkdown
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Render(MealExportModel m, MealExportStrings s)
    {
        var sb = new StringBuilder();

        // --- Header -----------------------------------------------------------
        sb.Append("# ").AppendLine(s.Title);
        sb.AppendLine();
        sb.AppendLine(s.Intro);
        sb.AppendLine();
        sb.Append("- ").Append(s.GeneratedLabel).Append(": ")
          .Append(m.GeneratedLocalDate.ToString("yyyy-MM-dd", Inv)).Append(' ')
          .AppendLine(m.GeneratedLocalTime.ToString("HH:mm", Inv));
        sb.Append("- ").Append(s.TimeZoneLabel).Append(": ").AppendLine(m.TimeZoneId);
        sb.Append("- ").Append(s.LanguageLabel).Append(": ").AppendLine(m.LanguageLabel);
        sb.Append("- ").Append(s.RangeLabel).Append(": ")
          .Append(m.From is { } f ? f.ToString("yyyy-MM-dd", Inv) : s.RangeOpenStart)
          .Append(" … ").AppendLine(m.To.ToString("yyyy-MM-dd", Inv));
        sb.Append("- ").Append(s.TotalMealsLabel).Append(": ").AppendLine(m.TotalMeals.ToString(Inv));
        sb.AppendLine();

        // --- Legend ---------------------------------------------------------
        sb.Append("## ").AppendLine(s.LegendHeading);
        sb.AppendLine();
        sb.AppendLine(s.LegendPortionIntro);
        sb.AppendLine();
        foreach (var size in PortionSizes.All)
        {
            sb.Append("- **").Append(s.SizeLabel(size)).Append("** — ").AppendLine(s.SizeLegend(size));
        }

        sb.AppendLine();
        sb.AppendLine(s.LegendGramsNote);
        sb.AppendLine();
        sb.AppendLine(s.LegendGroupsIntro);
        sb.AppendLine();
        foreach (var key in MealGroupKeys.Seed)
        {
            sb.Append("- `").Append(key).Append("` — ").AppendLine(s.GroupLabel(key));
        }

        sb.AppendLine();

        // --- Analysis -----------------------------------------------------
        sb.Append("## ").AppendLine(s.AnalysisHeading);
        sb.AppendLine();
        sb.Append("- ").Append(s.TotalMealsLabel).Append(": ").AppendLine(m.TotalMeals.ToString(Inv));
        sb.Append("- ").Append(s.DaysWithEntriesLabel).Append(": ").AppendLine(m.DaysWithEntries.ToString(Inv));
        sb.Append("- ").Append(s.StreakLabel).Append(": ").AppendLine(m.Streak.ToString(Inv));
        sb.AppendLine();

        if (m.PerDay.Count > 0)
        {
            sb.Append("### ").AppendLine(s.PerDayHeading);
            sb.AppendLine();
            sb.Append("| ").Append(s.ColDate).Append(" | ").Append(s.ColCount).AppendLine(" |");
            sb.AppendLine("| --- | ---: |");
            foreach (var d in m.PerDay)
            {
                sb.Append("| ").Append(d.Date.ToString("yyyy-MM-dd", Inv))
                  .Append(" | ").Append(d.Count.ToString(Inv)).AppendLine(" |");
            }

            sb.AppendLine();
        }

        if (m.ByGroup.Count > 0)
        {
            sb.Append("### ").AppendLine(s.ByGroupHeading);
            sb.AppendLine();
            sb.Append("| ").Append(s.ColGroup).Append(" | ").Append(s.ColCount).AppendLine(" |");
            sb.AppendLine("| --- | ---: |");
            foreach (var g in m.ByGroup)
            {
                sb.Append("| ").Append(g.GroupKey is { } k ? s.GroupLabel(k) : s.NoGroup)
                  .Append(" | ").Append(g.Count.ToString(Inv)).AppendLine(" |");
            }

            sb.AppendLine();
        }

        if (m.Tags.Count > 0)
        {
            sb.Append("### ").AppendLine(s.TagsHeading);
            sb.AppendLine();
            sb.Append("| ").Append(s.ColTags).Append(" | ").Append(s.ColCount).AppendLine(" |");
            sb.AppendLine("| --- | ---: |");
            foreach (var t in m.Tags)
            {
                sb.Append("| #").Append(MdCell(t.Tag))
                  .Append(" | ").Append(t.Count.ToString(Inv)).AppendLine(" |");
            }

            sb.AppendLine();
        }

        if (m.MissingDays is { Count: > 0 } listed)
        {
            sb.Append("### ").AppendLine(s.MissingDaysHeading);
            sb.AppendLine();
            foreach (var d in listed)
            {
                sb.Append("- ").AppendLine(d.ToString("yyyy-MM-dd", Inv));
            }

            sb.AppendLine();
        }
        else if (m.MissingDaysTooMany is { } tooMany)
        {
            sb.Append("### ").AppendLine(s.MissingDaysHeading);
            sb.AppendLine();
            sb.AppendLine(s.MissingDaysSummary(
                tooMany.Count,
                tooMany.First.ToString("yyyy-MM-dd", Inv),
                tooMany.Last.ToString("yyyy-MM-dd", Inv)));
            sb.AppendLine();
        }

        // --- Entries -------------------------------------------------------
        sb.Append("## ").AppendLine(s.EntriesHeading);
        sb.AppendLine();

        if (m.Entries.Count == 0)
        {
            sb.AppendLine(s.NoEntries);
            return sb.ToString();
        }

        sb.Append("| ").Append(s.ColDate)
          .Append(" | ").Append(s.ColTime)
          .Append(" | ").Append(s.ColName)
          .Append(" | ").Append(s.ColGroup)
          .Append(" | ").Append(s.ColPortion)
          .Append(" | ").Append(s.ColTags)
          .Append(" | ").Append(s.ColNotes)
          .AppendLine(" |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");

        foreach (var e in m.Entries)
        {
            var portion = e.PortionSize is { } sz
                ? s.SizeLabel(sz)
                : e.PortionGrams is { } g ? $"{g.ToString(Inv)} g" : "";

            sb.Append("| ").Append(e.LocalDate.ToString("yyyy-MM-dd", Inv))
              .Append(" | ").Append(e.LocalTime.ToString("HH:mm", Inv))
              .Append(" | ").Append(MdCell(e.Name))
              .Append(" | ").Append(e.GroupKey is { } gk ? s.GroupLabel(gk) : "")
              .Append(" | ").Append(portion)
              .Append(" | ").Append(MdCell(string.Join(", ", e.Tags)))
              .Append(" | ").Append(MdCell(e.Notes))
              .AppendLine(" |");
        }

        return sb.ToString();
    }

    /// <summary>Make user text safe for a Markdown table cell.</summary>
    private static string MdCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\': sb.Append("\\\\"); break;
                case '|': sb.Append("\\|"); break;
                case '\r' or '\n': sb.Append(' '); break;
                default: sb.Append(ch); break;
            }
        }

        return sb.ToString().Trim();
    }
}
