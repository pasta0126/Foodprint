using Bunit;
using Foodprint.Web.Components.Shared;

namespace Foodprint.Tests.DesignSystem;

public class IconTests : TestContext
{
    [Fact]
    public void Decorative_by_default_is_hidden_from_assistive_tech()
    {
        var cut = RenderComponent<Icon>(p => p.Add(i => i.Name, "edit"));

        var svg = cut.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.False(svg.HasAttribute("role"));
        Assert.Empty(cut.FindAll("title"));
    }

    [Fact]
    public void Titled_icon_exposes_an_accessible_name()
    {
        var cut = RenderComponent<Icon>(p => p
            .Add(i => i.Name, "delete")
            .Add(i => i.Title, "Delete entry"));

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.False(svg.HasAttribute("aria-hidden"));
        Assert.Equal("Delete entry", cut.Find("title").TextContent);
    }

    [Fact]
    public void Unknown_name_still_renders_an_svg()
    {
        var cut = RenderComponent<Icon>(p => p.Add(i => i.Name, "does-not-exist"));
        Assert.NotNull(cut.Find("svg"));
    }
}
