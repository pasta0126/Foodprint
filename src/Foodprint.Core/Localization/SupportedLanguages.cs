using System.Globalization;

namespace Foodprint.Core.Localization;

/// <summary>The three UI languages Foodprint ships from the MVP. Spanish is the fallback.</summary>
public static class SupportedLanguages
{
    public const string Catalan = "ca";
    public const string Spanish = "es";
    public const string English = "en";

    public const string Default = Spanish;

    public static readonly IReadOnlyList<string> All = new[] { Catalan, Spanish, English };

    public static bool IsSupported(string? code) => code is not null && All.Contains(code);

    public static readonly IReadOnlyList<CultureInfo> Cultures =
        All.Select(c => new CultureInfo(c)).ToList();

    /// <summary>Best supported match for a culture name, or null if none.</summary>
    public static string? Match(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return null;
        }

        var primary = cultureName.Split('-', '_')[0].ToLowerInvariant();
        return All.Contains(primary) ? primary : null;
    }
}
