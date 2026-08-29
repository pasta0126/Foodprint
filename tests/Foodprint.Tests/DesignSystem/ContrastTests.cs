using System.Globalization;

namespace Foodprint.Tests.DesignSystem;

/// <summary>
/// Guards requirement design-system / "Light and dark themes": every text/background
/// pairing must meet WCAG AA (4.5:1 body, 3:1 large text) in both themes. The token
/// values here mirror wwwroot/app.css; if you change a colour there, change it here.
/// </summary>
public class ContrastTests
{
    private const double BodyAa = 4.5;
    private const double LargeAa = 3.0;

    public static TheoryData<string, string, string, double> Pairings() => new()
    {
        // name, foreground, background, required ratio
        { "light: text on surface",        "#1c1e21", "#ffffff", BodyAa },
        { "light: text on surface-2",      "#1c1e21", "#f4f5f7", BodyAa },
        { "light: text on surface-3",      "#1c1e21", "#e9ebef", BodyAa },
        { "light: muted on surface",       "#565b63", "#ffffff", BodyAa },
        { "light: primary-text on primary","#ffffff", "#1f6f43", BodyAa },
        { "light: white on danger",        "#ffffff", "#b21f2d", BodyAa },
        { "light: primary on surface (lg)","#1f6f43", "#ffffff", LargeAa },

        { "dark: text on surface",         "#e9ebee", "#16181c", BodyAa },
        { "dark: text on surface-2",       "#e9ebee", "#1e2126", BodyAa },
        { "dark: text on surface-3",       "#e9ebee", "#282c33", BodyAa },
        { "dark: muted on surface",        "#a8afba", "#16181c", BodyAa },
        { "dark: primary-text on primary", "#0c1a12", "#4cbf82", BodyAa },
        { "dark: white on danger",         "#ffffff", "#e05563", LargeAa },
        { "dark: primary on surface (lg)", "#4cbf82", "#16181c", LargeAa },
    };

    [Theory]
    [MemberData(nameof(Pairings))]
    public void Pairing_meets_wcag_aa(string name, string fg, string bg, double required)
    {
        var ratio = ContrastRatio(fg, bg);
        Assert.True(ratio >= required, $"{name}: {ratio:F2}:1 < {required}:1");
    }

    private static double ContrastRatio(string hexA, string hexB)
    {
        var l1 = RelativeLuminance(hexA);
        var l2 = RelativeLuminance(hexB);
        var (hi, lo) = l1 >= l2 ? (l1, l2) : (l2, l1);
        return (hi + 0.05) / (lo + 0.05);
    }

    private static double RelativeLuminance(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Channel(int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber));
        var g = Channel(int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber));
        var b = Channel(int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber));
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;

        static double Channel(int v)
        {
            var c = v / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }
}
