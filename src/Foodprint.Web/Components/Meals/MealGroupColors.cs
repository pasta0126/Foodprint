namespace Foodprint.Web.Components.Meals;

/// <summary>A fixed, deterministic accent colour per meal-group key, used to tint quick-add cards.</summary>
public static class MealGroupColors
{
    private const string Fallback = "#64748b"; // slate

    private static readonly IReadOnlyDictionary<string, string> ByKey = new Dictionary<string, string>
    {
        ["breakfast"] = "#d97706", // amber
        ["lunch"] = "#16a34a",     // green
        ["dinner"] = "#6366f1",    // indigo
        ["snack"] = "#db2777",     // pink
        ["other"] = Fallback,
    };

    public static string For(string? key) =>
        key is not null && ByKey.TryGetValue(key, out var colour) ? colour : Fallback;
}
