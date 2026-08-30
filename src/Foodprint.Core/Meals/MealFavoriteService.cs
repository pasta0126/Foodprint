using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Meals;

/// <summary>The reusable parts of an entry, as submitted, used to create or update a favorite.</summary>
public sealed class FavoriteDraft
{
    public string Name { get; set; } = "";
    public string? PortionSize { get; set; }
    public int? PortionGrams { get; set; }
    public int? MealGroupId { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = [];
}

public sealed record MealFavoriteView(
    Guid Id,
    string Name,
    string? PortionSize,
    int? PortionGrams,
    int? MealGroupId,
    string? MealGroupKey,
    IReadOnlyList<string> Tags);

/// <summary>Favorites for one meal group (or the "no group" bucket when <see cref="Key"/> is null).</summary>
public sealed record FavoriteGroup(int? MealGroupId, string? Key, IReadOnlyList<MealFavoriteView> Favorites);

/// <summary>Per-user saved meal templates. Every operation is scoped to one user.</summary>
public sealed class MealFavoriteService(AppDbContext db, TimeProvider clock)
{
    /// <summary>Creates a favorite, or updates the matching one (same user + normalized name + meal group) in place.</summary>
    public async Task<MealFavorite> SaveAsync(Guid userId, FavoriteDraft draft, CancellationToken ct = default)
    {
        var name = draft.Name.Trim();
        var normalized = name.ToLowerInvariant();
        var tagsCsv = ToCsv(draft.Tags);
        var now = clock.GetUtcNow().UtcDateTime;

        var existing = await db.MealFavorites.FirstOrDefaultAsync(
            f => f.UserId == userId && f.NameNormalized == normalized && f.MealGroupId == draft.MealGroupId, ct);

        if (existing is null)
        {
            existing = new MealFavorite
            {
                UserId = userId,
                Name = name,
                NameNormalized = normalized,
                MealGroupId = draft.MealGroupId,
                CreatedAt = now,
            };
            db.MealFavorites.Add(existing);
        }

        existing.Name = name;
        existing.PortionSize = draft.PortionSize;
        existing.PortionGrams = draft.PortionGrams;
        existing.TagsCsv = tagsCsv;
        existing.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    /// <summary>The user's favorites grouped by meal group, groups in catalog order, "no group" last.</summary>
    public async Task<IReadOnlyList<FavoriteGroup>> ListGroupedAsync(Guid userId, CancellationToken ct = default)
    {
        var favorites = await db.MealFavorites
            .Where(f => f.UserId == userId)
            .Include(f => f.MealGroup)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

        return favorites
            .GroupBy(f => f.MealGroupId)
            .Select(g => new
            {
                g.Key,
                Group = g.First().MealGroup,
                Items = g.Select(ToView).ToList(),
            })
            .OrderBy(x => x.Group is null ? int.MaxValue : x.Group.SortOrder)
            .Select(x => new FavoriteGroup(x.Key, x.Group?.Key, x.Items))
            .ToList();
    }

    public async Task<MealFavoriteView?> GetAsync(Guid userId, Guid favoriteId, CancellationToken ct = default)
    {
        var favorite = await db.MealFavorites
            .Where(f => f.UserId == userId)
            .Include(f => f.MealGroup)
            .FirstOrDefaultAsync(f => f.Id == favoriteId, ct);

        return favorite is null ? null : ToView(favorite);
    }

    /// <summary>Permanently deletes a favorite the user owns. False when it does not exist / is not theirs.</summary>
    public async Task<bool> DeleteAsync(Guid userId, Guid favoriteId, CancellationToken ct = default)
    {
        var deleted = await db.MealFavorites
            .Where(f => f.UserId == userId && f.Id == favoriteId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    private static string ToCsv(IEnumerable<string> tags) =>
        string.Join(", ", MealEntryRules.NormalizeTags(tags));

    private static IReadOnlyList<string> FromCsv(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    internal static MealFavoriteView ToView(MealFavorite f) => new(
        f.Id, f.Name, f.PortionSize, f.PortionGrams,
        f.MealGroupId, f.MealGroup?.Key, FromCsv(f.TagsCsv));
}
