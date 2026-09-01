using Bunit;
using Foodprint.Core.Meals;
using Foodprint.Web.Components.Meals;
using Microsoft.Extensions.DependencyInjection;

namespace Foodprint.Tests.DesignSystem;

public class QuickAddCardsTests : TestContext
{
    public QuickAddCardsTests() => Services.AddLocalization();

    private static MealFavoriteView Fav(string name, string? group, int? groupId) =>
        new(Guid.NewGuid(), name, "small", null, groupId, group, ["quick"], null);

    [Fact]
    public void Renders_a_group_per_meal_group_and_a_delete_form_per_card()
    {
        IReadOnlyList<FavoriteGroup> groups =
        [
            new(1, "breakfast", [Fav("Yogurt", "breakfast", 1), Fav("Toast", "breakfast", 1)]),
            new(2, "lunch", [Fav("Salad", "lunch", 2)]),
        ];

        var cut = RenderComponent<QuickAddCards>(p => p.Add(c => c.Groups, groups));

        Assert.Equal(2, cut.FindAll(".fp-quickadd__group").Count);
        Assert.Equal(3, cut.FindAll(".fp-quickcard").Count);
        Assert.Equal(3, cut.FindAll("form[action*='/favorites/'][action$='/delete']").Count);
        Assert.Contains("/?from=", cut.Find(".fp-quickcard__pick").GetAttribute("href"));
    }

    [Fact]
    public void Renders_nothing_when_there_are_no_favorites()
    {
        var cut = RenderComponent<QuickAddCards>(p => p.Add(c => c.Groups, Array.Empty<FavoriteGroup>()));
        Assert.Empty(cut.FindAll(".fp-quickadd"));
    }
}
