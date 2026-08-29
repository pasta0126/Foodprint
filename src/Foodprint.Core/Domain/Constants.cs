namespace Foodprint.Core.Domain;

public static class PortionSizes
{
    public const string Small = "small";
    public const string Medium = "medium";
    public const string Large = "large";

    public static readonly IReadOnlyList<string> All = new[] { Small, Medium, Large };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}

public static class PortionGrams
{
    public const int Min = 1;
    public const int Max = 5000;
}

public static class MealGroupKeys
{
    /// <summary>The catalog seeded into a fresh database, in display order.</summary>
    public static readonly IReadOnlyList<string> Seed = new[]
    {
        "breakfast", "lunch", "dinner", "snack", "other",
    };
}
