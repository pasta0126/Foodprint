using Foodprint.Web.Components.Shared;

namespace Foodprint.Tests.DesignSystem;

public class AvatarIdentityTests
{
    [Fact]
    public void Color_is_deterministic_for_a_key()
    {
        var a = AvatarIdentity.Color("11111111-1111-1111-1111-111111111111");
        var b = AvatarIdentity.Color("11111111-1111-1111-1111-111111111111");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Color_is_always_a_palette_member()
    {
        foreach (var key in new[] { "alex@example.com", "", "z", Guid.NewGuid().ToString() })
        {
            Assert.Contains(AvatarIdentity.Color(key), AvatarIdentity.Palette);
        }
    }

    [Theory]
    [InlineData("Alex", "alex@example.com", "A")]
    [InlineData("  émile", "x@example.com", "É")]
    [InlineData("", "bob@example.com", "B")]
    [InlineData(null, "carol@example.com", "C")]
    [InlineData("", "", "?")]
    public void Initial_prefers_name_then_email(string? name, string? email, string expected)
    {
        Assert.Equal(expected, AvatarIdentity.Initial(name, email));
    }
}
