using Foodprint.Core.Meals;
using Foodprint.Web.Components.Meals;

namespace Foodprint.Tests.Meals;

public class MealEntryFormModelTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    private static MealEntryFormModel Base() => new()
    {
        Name = "Toast",
        EatenAtLocal = new DateTime(2026, 8, 30, 9, 0, 0),
    };

    [Fact]
    public void Grams_wins_over_a_selected_size()
    {
        var model = Base();
        model.PortionChoice = "large";
        model.PortionGrams = 250;

        var input = model.ToInput(Utc);

        Assert.Equal(250, input.PortionGrams);
        Assert.Null(input.PortionSize);
    }

    [Fact]
    public void Named_size_is_kept_when_no_grams()
    {
        var model = Base();
        model.PortionChoice = "very-large";

        var input = model.ToInput(Utc);

        Assert.Equal("very-large", input.PortionSize);
        Assert.Null(input.PortionGrams);
    }

    [Fact]
    public void Empty_choice_and_no_grams_means_no_portion()
    {
        var input = Base().ToInput(Utc);

        Assert.Null(input.PortionSize);
        Assert.Null(input.PortionGrams);
    }

    [Fact]
    public void FromView_round_trips_a_named_size()
    {
        var view = new MealEntryView(
            Guid.NewGuid(), "Toast", DateTime.UtcNow, "medium", null, null, null, null, []);

        var model = MealEntryFormModel.FromView(view, Utc);

        Assert.Equal("medium", model.PortionChoice);
        Assert.Null(model.PortionGrams);
    }
}
