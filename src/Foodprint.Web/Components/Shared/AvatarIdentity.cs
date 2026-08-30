using System.Globalization;

namespace Foodprint.Web.Components.Shared;

/// <summary>
/// Turns a stable user key into a deterministic avatar: one background color from a
/// fixed palette (all meet WCAG AA against white text) plus a display initial.
/// </summary>
public static class AvatarIdentity
{
    /// <summary>Dark backgrounds; contrast ratio against #fff is >= 4.5:1 for each.</summary>
    public static readonly IReadOnlyList<string> Palette = new[]
    {
        "#1d4ed8", "#b91c1c", "#15803d", "#a16207", "#7c3aed", "#0f766e",
        "#be185d", "#4d7c0f", "#0369a1", "#6d28d9", "#c2410c", "#334155",
    };

    /// <summary>Same key always maps to the same palette entry.</summary>
    public static string Color(string key)
    {
        // FNV-1a over UTF-16 code units: stable, no allocations, good spread.
        uint hash = 2166136261;
        foreach (var ch in key ?? "")
        {
            hash = (hash ^ ch) * 16777619;
        }

        return Palette[(int)(hash % (uint)Palette.Count)];
    }

    /// <summary>First letter of the name, or of the email, upper-cased; "?" if neither has one.</summary>
    public static string Initial(string? displayName, string? email)
    {
        var source = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
        var trimmed = source?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return "?";
        }

        var runes = trimmed.EnumerateRunes();
        runes.MoveNext();
        return runes.Current.ToString().ToUpper(CultureInfo.InvariantCulture);
    }
}
