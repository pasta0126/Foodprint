using Bunit;
using Foodprint.Web.Auth;
using Foodprint.Web.Components.Layout;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Foodprint.Tests.DesignSystem;

public class AppShellTests : TestContext
{
    [Fact]
    public void Header_shows_nav_and_identity_only_no_language_theme_or_signout_controls()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new(System.Security.Claims.ClaimTypes.NameIdentifier, "11111111-1111-1111-1111-111111111111"),
            new(System.Security.Claims.ClaimTypes.Name, "Alex"),
            new(System.Security.Claims.ClaimTypes.Email, "alex@example.com"),
        ], "test"));
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        Services.AddSingleton<IHttpContextAccessor>(accessor);
        Services.AddSingleton(new CurrentUser(accessor));
        Services.AddLocalization();

        var cut = RenderComponent<AppShell>();
        var header = cut.Find(".fp-shell__header");

        Assert.NotNull(header.QuerySelector("nav.fp-shell__nav a[href='/']"));
        Assert.NotNull(header.QuerySelector(".fp-identity"));
        // No language <select>, no theme control, and the only sign-out lives inside
        // the identity menu (not a bare header form).
        Assert.Null(header.QuerySelector("select"));
        Assert.Null(header.QuerySelector("#fp-theme-control"));
        Assert.Null(header.QuerySelector("nav.fp-shell__nav a[href='/profile']"));
    }
}
