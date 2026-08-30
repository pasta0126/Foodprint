using Foodprint.Core.Meals;

namespace Foodprint.Tests.Meals;

public class MealGroupSuggestionTests
{
    private static readonly IReadOnlyList<MealGroupOption> Full =
    [
        new(1, "breakfast", 0),
        new(2, "lunch", 1),
        new(3, "dinner", 2),
        new(4, "snack", 3),
        new(5, "other", 4),
    ];

    [Theory]
    [InlineData(8, 30, 1)]   // breakfast
    [InlineData(13, 0, 2)]   // lunch
    [InlineData(20, 30, 3)]  // dinner
    [InlineData(16, 0, 4)]   // between lunch and dinner -> snack
    [InlineData(3, 0, 4)]    // small hours -> snack
    public void Maps_time_of_day_to_group(int hour, int minute, int expectedId)
    {
        var id = MealGroupSuggestion.ForLocalTime(new TimeOnly(hour, minute), Full);
        Assert.Equal(expectedId, id);
    }

    [Fact]
    public void Falls_back_to_snack_when_the_band_group_is_not_active()
    {
        IReadOnlyList<MealGroupOption> noBreakfast = [new(2, "lunch", 0), new(4, "snack", 1)];

        var id = MealGroupSuggestion.ForLocalTime(new TimeOnly(8, 0), noBreakfast);

        Assert.Equal(4, id);
    }

    [Fact]
    public void Returns_null_when_neither_band_nor_snack_is_active()
    {
        IReadOnlyList<MealGroupOption> only = [new(9, "other", 0)];

        Assert.Null(MealGroupSuggestion.ForLocalTime(new TimeOnly(8, 0), only));
    }
}
