using System.ComponentModel.DataAnnotations;
using Foodprint.Core.Meals;

namespace Foodprint.Web.Components.Meals;

public enum PortionMode { None, Size, Grams }

/// <summary>Form-bound shape of a meal entry. Times are in the user's profile time zone.</summary>
public sealed class MealEntryFormModel
{
    [Required, StringLength(MealEntryRules.NameMax, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required]
    public DateTime EatenAtLocal { get; set; }

    public PortionMode PortionMode { get; set; } = PortionMode.None;

    public string? PortionSize { get; set; }

    [Range(Foodprint.Core.Domain.PortionGrams.Min, Foodprint.Core.Domain.PortionGrams.Max)]
    public int? PortionGrams { get; set; }

    public int? MealGroupId { get; set; }

    [StringLength(MealEntryRules.NotesMax)]
    public string? Notes { get; set; }

    /// <summary>Comma- or space-separated free text; normalized server-side.</summary>
    public string TagsText { get; set; } = "";

    public IReadOnlyList<string> ParseTags() =>
        (TagsText ?? "").Split([',', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public MealEntryInput ToInput(TimeZoneInfo zone) => new()
    {
        Name = Name,
        EatenAtUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(EatenAtLocal, DateTimeKind.Unspecified), zone),
        PortionSize = PortionMode == PortionMode.Size ? PortionSize : null,
        PortionGrams = PortionMode == PortionMode.Grams ? PortionGrams : null,
        MealGroupId = MealGroupId,
        Notes = Notes,
        Tags = ParseTags(),
    };

    public static MealEntryFormModel FromView(MealEntryView v, TimeZoneInfo zone) => new()
    {
        Name = v.Name,
        EatenAtLocal = TimeZoneInfo.ConvertTimeFromUtc(v.EatenAtUtc, zone),
        PortionMode = v.PortionSize is not null ? PortionMode.Size
            : v.PortionGrams is not null ? PortionMode.Grams
            : PortionMode.None,
        PortionSize = v.PortionSize,
        PortionGrams = v.PortionGrams,
        MealGroupId = v.MealGroupId,
        Notes = v.Notes,
        TagsText = string.Join(", ", v.Tags),
    };
}
