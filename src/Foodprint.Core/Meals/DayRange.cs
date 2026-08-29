namespace Foodprint.Core.Meals;

/// <summary>The half-open UTC interval [StartUtc, EndUtc) covering one local calendar day.</summary>
public readonly record struct DayRange(DateTime StartUtc, DateTime EndUtc)
{
    /// <summary>
    /// The UTC window for <paramref name="date"/> as a calendar day in <paramref name="zone"/>.
    /// Handles DST: on a spring-forward day the window is 23h, on fall-back 25h.
    /// </summary>
    public static DayRange For(DateOnly date, TimeZoneInfo zone)
    {
        var startLocal = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DayRange(
            TimeZoneInfo.ConvertTimeToUtc(startLocal, zone),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, zone));
    }

    public bool Contains(DateTime utc) => utc >= StartUtc && utc < EndUtc;

    /// <summary>The local calendar date that a UTC instant falls on in <paramref name="zone"/>.</summary>
    public static DateOnly LocalDate(DateTime utc, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone));
}
