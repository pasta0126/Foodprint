using Bunit;
using Foodprint.Web.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace Foodprint.Tests.DesignSystem;

public class FpComponentTests : TestContext
{
    [Fact]
    public void FpButton_activates_on_click_and_enter_space_natively()
    {
        var clicks = 0;
        var cut = RenderComponent<FpButton>(p => p
            .Add(b => b.OnClick, EventCallback.Factory.Create(this, () => clicks++))
            .AddChildContent("Save"));

        var button = cut.Find("button.fp-btn");
        button.Click();

        Assert.Equal(1, clicks);
        // A real <button> is keyboard-operable by the browser; the test asserts we
        // render a <button>, not a clickable <div>.
        Assert.Equal("button", button.NodeName, ignoreCase: true);
        Assert.Contains("fp-btn--primary", button.ClassList);
    }

    [Fact]
    public void FpButton_renders_as_anchor_when_href_given()
    {
        var cut = RenderComponent<FpButton>(p => p
            .Add(b => b.Href, "/history")
            .AddChildContent("History"));

        var link = cut.Find("a.fp-btn");
        Assert.Equal("/history", link.GetAttribute("href"));
    }

    [Fact]
    public void FpInput_label_is_associated_with_the_input()
    {
        var cut = RenderComponent<FpInput>(p => p.Add(i => i.Label, "Meal name"));

        var label = cut.Find("label");
        var input = cut.Find("input");

        Assert.False(string.IsNullOrEmpty(input.Id));
        Assert.Equal(input.Id, label.GetAttribute("for"));
        Assert.Equal("Meal name", label.TextContent.Trim());
    }

    [Fact]
    public void FpInput_error_sets_aria_invalid_and_describedby()
    {
        var cut = RenderComponent<FpInput>(p => p
            .Add(i => i.Label, "Grams")
            .Add(i => i.Error, "Must be between 1 and 5000"));

        var input = cut.Find("input");
        var error = cut.Find(".fp-field__error");

        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(error.Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("alert", error.GetAttribute("role"));
    }

    [Fact]
    public void FpChip_remove_button_has_accessible_label()
    {
        var removed = false;
        var cut = RenderComponent<FpChip>(p => p
            .Add(c => c.OnRemove, EventCallback.Factory.Create(this, () => removed = true))
            .Add(c => c.RemoveLabel, "Remove tag lunch")
            .AddChildContent("lunch"));

        var remove = cut.Find("button[aria-label='Remove tag lunch']");
        remove.Click();

        Assert.True(removed);
    }
}
