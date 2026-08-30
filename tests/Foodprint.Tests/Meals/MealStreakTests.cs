using Foodprint.Core.Meals;

namespace Foodprint.Tests.Meals;

public class MealStreakTests
{
    private static readonly DateOnly Today = new(2026, 8, 30);

    [Fact]
    public void Counts_consecutive_days_ending_today()
    {
        var days = new HashSet<DateOnly> { Today, Today.AddDays(-1), Today.AddDays(-2) };
        Assert.Equal(3, MealStreak.Current(days, Today));
    }

    [Fact]
    public void Zero_when_nothing_logged_today()
    {
        var days = new HashSet<DateOnly> { Today.AddDays(-1), Today.AddDays(-2) };
        Assert.Equal(0, MealStreak.Current(days, Today));
    }

    [Fact]
    public void Stops_at_the_first_gap()
    {
        var days = new HashSet<DateOnly> { Today, Today.AddDays(-1), Today.AddDays(-3), Today.AddDays(-4) };
        Assert.Equal(2, MealStreak.Current(days, Today));
    }

    [Fact]
    public void Empty_set_is_zero()
    {
        Assert.Equal(0, MealStreak.Current(new HashSet<DateOnly>(), Today));
    }
}
