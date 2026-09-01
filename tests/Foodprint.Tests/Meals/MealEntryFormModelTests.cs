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
    public void FromFavorite_prefills_name_portion_group_tags_and_sets_time_to_now()
    {
        var fav = new MealFavoriteView(Guid.NewGuid(), "Greek yogurt", "small", null, 1, "breakfast", ["quick", "protein"], "with honey");
        var now = new DateTime(2026, 8, 30, 8, 15, 0);

        var model = MealEntryFormModel.FromFavorite(fav, now);

        Assert.Equal("Greek yogurt", model.Name);
        Assert.Equal("small", model.PortionChoice);
        Assert.Equal(1, model.MealGroupId);
        Assert.Equal("quick, protein", model.TagsText);
        Assert.Equal(now, model.EatenAtLocal);
        Assert.Equal("with honey", model.Notes);
        Assert.False(model.SaveFavorite);
    }

    [Fact]
    public void ToFavoriteDraft_carries_the_chosen_portion_tags_and_notes()
    {
        var model = Base();
        model.PortionChoice = "large";
        model.MealGroupId = 2;
        model.TagsText = "home, quick";
        model.Notes = "extra crispy";

        var draft = model.ToFavoriteDraft();

        Assert.Equal("Toast", draft.Name);
        Assert.Equal("large", draft.PortionSize);
        Assert.Null(draft.PortionGrams);
        Assert.Equal(2, draft.MealGroupId);
        Assert.Equal(["home", "quick"], draft.Tags);
        Assert.Equal("extra crispy", draft.Notes);
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
