using System.ComponentModel.DataAnnotations;
using Foodprint.Core.Meals;

namespace Foodprint.Web.Components.Meals;

/// <summary>Form-bound shape of a meal entry. Times are in the user's profile time zone.</summary>
public sealed class MealEntryFormModel
{
    [Required, StringLength(MealEntryRules.NameMax, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required]
    public DateTime EatenAtLocal { get; set; }

    /// <summary>Empty string = no named size; otherwise one of <see cref="Foodprint.Core.Domain.PortionSizes"/>.</summary>
    public string PortionChoice { get; set; } = "";

    [Range(Foodprint.Core.Domain.PortionGrams.Min, Foodprint.Core.Domain.PortionGrams.Max)]
    public int? PortionGrams { get; set; }

    public int? MealGroupId { get; set; }

    [StringLength(MealEntryRules.NotesMax)]
    public string? Notes { get; set; }

    /// <summary>Comma- or space-separated free text; normalized server-side.</summary>
    public string TagsText { get; set; } = "";

    /// <summary>When true, saving the entry also creates/updates a favorite (see <see cref="MealFavoriteService"/>).</summary>
    public bool SaveFavorite { get; set; }

    public IReadOnlyList<string> ParseTags() =>
        (TagsText ?? "").Split([',', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public MealEntryInput ToInput(TimeZoneInfo zone) => new()
    {
        Name = Name,
        EatenAtUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(EatenAtLocal, DateTimeKind.Unspecified), zone),
        // Grams (from the disclosure) wins; otherwise the named size. Never both.
        PortionGrams = PortionGrams,
        PortionSize = PortionGrams is null && !string.IsNullOrEmpty(PortionChoice) ? PortionChoice : null,
        MealGroupId = MealGroupId,
        Notes = Notes,
        Tags = ParseTags(),
    };

    public static MealEntryFormModel FromView(MealEntryView v, TimeZoneInfo zone) => new()
    {
        Name = v.Name,
        EatenAtLocal = TimeZoneInfo.ConvertTimeFromUtc(v.EatenAtUtc, zone),
        PortionChoice = v.PortionSize ?? "",
        PortionGrams = v.PortionGrams,
        MealGroupId = v.MealGroupId,
        Notes = v.Notes,
        TagsText = string.Join(", ", v.Tags),
    };

    /// <summary>Pre-fill from a favorite: its name, portion, group and tags, with eaten-at set to now.</summary>
    public static MealEntryFormModel FromFavorite(MealFavoriteView f, DateTime nowLocal) => new()
    {
        Name = f.Name,
        EatenAtLocal = nowLocal,
        PortionChoice = f.PortionSize ?? "",
        PortionGrams = f.PortionGrams,
        MealGroupId = f.MealGroupId,
        TagsText = string.Join(", ", f.Tags),
    };

    public FavoriteDraft ToFavoriteDraft() => new()
    {
        Name = Name,
        PortionGrams = PortionGrams,
        PortionSize = PortionGrams is null && !string.IsNullOrEmpty(PortionChoice) ? PortionChoice : null,
        MealGroupId = MealGroupId,
        Tags = ParseTags(),
    };
}
