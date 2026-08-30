namespace Foodprint.Core.Meals;

/// <summary>
/// Suggests a meal group from the local time of day, used only to pre-select the
/// picker when creating a new entry. Pure; the caller supplies the active catalog.
/// </summary>
public static class MealGroupSuggestion
{
    /// <summary>Active group id matching the time-of-day band, falling back to snack, else null.</summary>
    public static int? ForLocalTime(TimeOnly localTime, IReadOnlyList<MealGroupOption> active)
    {
        var preferred = BandKey(localTime.Hour);
        return Resolve(preferred, active) ?? Resolve("snack", active);
    }

    private static string BandKey(int hour) => hour switch
    {
        >= 5 and < 11 => "breakfast",
        >= 11 and < 16 => "lunch",
        >= 19 => "dinner",
        _ => "snack",
    };

    private static int? Resolve(string key, IReadOnlyList<MealGroupOption> active)
    {
        foreach (var g in active)
        {
            if (g.Key == key)
            {
                return g.Id;
            }
        }

        return null;
    }
}
