using Foodprint.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Meals;

public sealed record MealGroupOption(int Id, string Key, int SortOrder);

/// <summary>Read access to the meal-group catalog. Mutations live only in the admin CLI.</summary>
public sealed class MealGroupService(AppDbContext db)
{
    /// <summary>Active groups for the entry picker, in display order.</summary>
    public async Task<IReadOnlyList<MealGroupOption>> ActiveAsync(CancellationToken ct = default) =>
        await db.MealGroups
            .Where(g => g.RetiredAt == null)
            .OrderBy(g => g.SortOrder)
            .Select(g => new MealGroupOption(g.Id, g.Key, g.SortOrder))
            .ToListAsync(ct);

    public async Task<bool> IsActiveAsync(int id, CancellationToken ct = default) =>
        await db.MealGroups.AnyAsync(g => g.Id == id && g.RetiredAt == null, ct);
}
