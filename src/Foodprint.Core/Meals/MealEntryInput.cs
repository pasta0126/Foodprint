using Foodprint.Core.Domain;

namespace Foodprint.Core.Meals;

/// <summary>Everything needed to create or update a meal entry, before validation.</summary>
public sealed class MealEntryInput
{
    public string Name { get; set; } = "";
    public DateTime EatenAtUtc { get; set; }
    public string? PortionSize { get; set; }
    public int? PortionGrams { get; set; }
    public int? MealGroupId { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = [];
}

public enum MealValidationError
{
    None,
    NameRequired,
    NameTooLong,
    EatenAtTooFarInFuture,
    NotesTooLong,
    PortionRequired,
    PortionBothProvided,
    PortionSizeInvalid,
    PortionGramsOutOfRange,
    TooManyTags,
    TagTooLong,
    UnknownMealGroup,
}

public sealed record MealValidationResult(MealValidationError Error)
{
    public bool Ok => Error == MealValidationError.None;
    public static readonly MealValidationResult Valid = new(MealValidationError.None);
}

public static class MealEntryRules
{
    public const int NameMax = 120;
    public const int NotesMax = 1000;
    public const int MaxTags = 10;
    public const int TagMin = 1;
    public const int TagMax = 30;
    public static readonly TimeSpan MaxFutureSkew = TimeSpan.FromHours(24);

    /// <summary>Trim + lower-case, drop blanks, de-duplicate, preserving first-seen order.</summary>
    public static List<string> NormalizeTags(IEnumerable<string> tags)
    {
        var seen = new HashSet<string>();
        var result = new List<string>();
        foreach (var raw in tags)
        {
            var t = raw.Trim().ToLowerInvariant();
            if (t.Length == 0 || !seen.Add(t))
            {
                continue;
            }

            result.Add(t);
        }

        return result;
    }

    /// <summary>Field-level validation shared by create and update. Does not check the meal group (needs the DB).</summary>
    public static MealValidationResult Validate(MealEntryInput input, DateTime nowUtc, IReadOnlyList<string> normalizedTags)
    {
        var name = input.Name?.Trim() ?? "";
        if (name.Length == 0)
        {
            return new(MealValidationError.NameRequired);
        }

        if (name.Length > NameMax)
        {
            return new(MealValidationError.NameTooLong);
        }

        if (input.EatenAtUtc > nowUtc + MaxFutureSkew)
        {
            return new(MealValidationError.EatenAtTooFarInFuture);
        }

        if ((input.Notes?.Length ?? 0) > NotesMax)
        {
            return new(MealValidationError.NotesTooLong);
        }

        if (input.PortionSize is null && input.PortionGrams is null)
        {
            return new(MealValidationError.PortionRequired);
        }

        if (input.PortionSize is not null && input.PortionGrams is not null)
        {
            return new(MealValidationError.PortionBothProvided);
        }

        if (input.PortionSize is not null && !PortionSizes.IsValid(input.PortionSize))
        {
            return new(MealValidationError.PortionSizeInvalid);
        }

        if (input.PortionGrams is { } grams && grams is < PortionGrams.Min or > PortionGrams.Max)
        {
            return new(MealValidationError.PortionGramsOutOfRange);
        }

        if (normalizedTags.Count > MaxTags)
        {
            return new(MealValidationError.TooManyTags);
        }

        if (normalizedTags.Any(t => t.Length is < TagMin or > TagMax))
        {
            return new(MealValidationError.TagTooLong);
        }

        return MealValidationResult.Valid;
    }
}
