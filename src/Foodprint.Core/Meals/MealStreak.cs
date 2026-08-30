namespace Foodprint.Core.Meals;

/// <summary>The current logging streak: consecutive local days with at least one entry, ending today.</summary>
public static class MealStreak
{
    /// <summary>How far back to load entries to be sure a streak has really ended.</summary>
    public const int LookbackDays = 400;

    public static int Current(IReadOnlySet<DateOnly> daysWithEntries, DateOnly today)
    {
        var streak = 0;
        for (var day = today; daysWithEntries.Contains(day); day = day.AddDays(-1))
        {
            streak++;
        }

        return streak;
    }
}
