using System.Globalization;

namespace Foodprint.Web.Localization;

/// <summary>Locale-aware formatting for the active request culture.</summary>
public static class LocalFormats
{
    private static CultureInfo C => CultureInfo.CurrentCulture;

    /// <summary>e.g. "Wed, 4 Mar 2026" / "mié, 4 mar 2026".</summary>
    public static string MediumDate(DateOnly date) => date.ToString("ddd, d MMM yyyy", C);

    /// <summary>e.g. "4 March 2026".</summary>
    public static string LongDate(DateOnly date) => date.ToString("d MMMM yyyy", C);

    /// <summary>Short time, e.g. "13:05" / "1:05 PM".</summary>
    public static string Time(DateTime local) => local.ToString("t", C);

    /// <summary>Localized full weekday name.</summary>
    public static string Weekday(DateOnly date) => C.DateTimeFormat.GetDayName(date.DayOfWeek);

    /// <summary>Localized short weekday name.</summary>
    public static string ShortWeekday(DateOnly date) => C.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);

    public static string Number(int value) => value.ToString("N0", C);

    /// <summary>e.g. "250 g".</summary>
    public static string Grams(int grams) => $"{Number(grams)} g";
}
