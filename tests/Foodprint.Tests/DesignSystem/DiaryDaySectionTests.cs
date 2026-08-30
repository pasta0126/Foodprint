using Bunit;
using Foodprint.Core.Meals;
using Foodprint.Web.Components.Meals;
using Microsoft.Extensions.DependencyInjection;

namespace Foodprint.Tests.DesignSystem;

public class DiaryDaySectionTests : TestContext
{
    public DiaryDaySectionTests() => Services.AddLocalization();

    private static MealEntryView Entry(string name, int hourUtc) =>
        new(Guid.NewGuid(), name, new DateTime(2026, 5, 10, hourUtc, 0, 0, DateTimeKind.Utc),
            "small", null, null, null, null, []);

    [Fact]
    public void Lists_the_days_entries_as_cards_in_given_order()
    {
        var day = new DiaryDay(new DateOnly(2026, 5, 10), [Entry("a", 8), Entry("b", 12), Entry("c", 18)]);

        var cut = RenderComponent<DiaryDaySection>(p => p
            .Add(c => c.Day, day)
            .Add(c => c.Zone, TimeZoneInfo.Utc));

        var names = cut.FindAll(".fp-entry__name").Select(e => e.TextContent.Trim());
        Assert.Equal(["a", "b", "c"], names);
        Assert.Contains("/?date=2026-05-10", cut.Find(".fp-daysection__head a").GetAttribute("href"));
    }

    [Fact]
    public void Shows_an_empty_state_for_a_day_with_no_entries()
    {
        var cut = RenderComponent<DiaryDaySection>(p => p
            .Add(c => c.Day, new DiaryDay(new DateOnly(2026, 5, 10), []))
            .Add(c => c.Zone, TimeZoneInfo.Utc));

        Assert.Empty(cut.FindAll(".fp-entry"));
        Assert.NotEmpty(cut.FindAll(".fp-card"));
    }
}
