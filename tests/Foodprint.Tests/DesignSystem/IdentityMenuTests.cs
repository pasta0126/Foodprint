using Bunit;
using Foodprint.Web.Auth;
using Foodprint.Web.Components.Layout;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Foodprint.Tests.DesignSystem;

public class IdentityMenuTests : TestContext
{
    private void Arrange(string displayName, string email)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new(System.Security.Claims.ClaimTypes.NameIdentifier, "11111111-1111-1111-1111-111111111111"),
            new(System.Security.Claims.ClaimTypes.Name, displayName),
            new(System.Security.Claims.ClaimTypes.Email, email),
        ], "test"));
        var accessor = new HttpContextAccessor { HttpContext = ctx };

        Services.AddSingleton<IHttpContextAccessor>(accessor);
        Services.AddSingleton(new CurrentUser(accessor));
        Services.AddLocalization();
    }

    [Fact]
    public void Renders_a_deterministic_avatar_and_the_account_menu()
    {
        Arrange("Alex", "alex@example.com");

        var first = RenderComponent<IdentityMenu>();
        var avatar1 = first.Find(".fp-avatar");

        var second = RenderComponent<IdentityMenu>();
        var avatar2 = second.Find(".fp-avatar");

        Assert.Equal("A", avatar1.TextContent.Trim());
        Assert.Equal(avatar1.GetAttribute("style"), avatar2.GetAttribute("style"));

        Assert.NotNull(first.Find("a[href='/profile']"));
        Assert.NotNull(first.Find("form[action='/auth/sign-out'] button[type=submit]"));
    }

    [Fact]
    public void Falls_back_to_email_initial_when_no_display_name()
    {
        Arrange("", "bob@example.com");

        var cut = RenderComponent<IdentityMenu>();

        Assert.Equal("B", cut.Find(".fp-avatar").TextContent.Trim());
    }
}
