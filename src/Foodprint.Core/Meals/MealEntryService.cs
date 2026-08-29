using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Meals;

public sealed record MealEntryView(
    Guid Id,
    string Name,
    DateTime EatenAtUtc,
    string? PortionSize,
    int? PortionGrams,
    int? MealGroupId,
    string? MealGroupKey,
    string? Notes,
    IReadOnlyList<string> Tags);

public sealed record MealMutationResult(MealValidationError Error, Guid EntryId = default, bool NotFound = false)
{
    public bool Ok => Error == MealValidationError.None && !NotFound;
    public static readonly MealMutationResult Missing = new(MealValidationError.None, NotFound: true);
}

/// <summary>Create / read / update / delete for meal entries. Every operation is scoped to one user.</summary>
public sealed class MealEntryService(AppDbContext db, MealGroupService groups, TimeProvider clock)
{
    public async Task<MealMutationResult> CreateAsync(Guid userId, MealEntryInput input, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var tags = MealEntryRules.NormalizeTags(input.Tags);

        var validation = MealEntryRules.Validate(input, now, tags);
        if (!validation.Ok)
        {
            return new(validation.Error);
        }

        if (input.MealGroupId is { } gid && !await groups.IsActiveAsync(gid, ct))
        {
            return new(MealValidationError.UnknownMealGroup);
        }

        var entry = new MealEntry
        {
            UserId = userId,
            Name = input.Name.Trim(),
            EatenAt = input.EatenAtUtc,
            PortionSize = input.PortionSize,
            PortionGrams = input.PortionGrams,
            MealGroupId = input.MealGroupId,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.MealEntries.Add(entry);
        await AttachTagsAsync(userId, entry, tags, ct);
        await db.SaveChangesAsync(ct);

        return new(MealValidationError.None, entry.Id);
    }

    public async Task<MealEntryView?> GetAsync(Guid userId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await Owned(userId)
            .Include(e => e.MealGroup)
            .Include(e => e.EntryTags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);

        return entry is null ? null : ToView(entry);
    }

    public async Task<MealMutationResult> UpdateAsync(Guid userId, Guid entryId, MealEntryInput input, CancellationToken ct = default)
    {
        var entry = await Owned(userId)
            .Include(e => e.EntryTags)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry is null)
        {
            return MealMutationResult.Missing;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var tags = MealEntryRules.NormalizeTags(input.Tags);

        var validation = MealEntryRules.Validate(input, now, tags);
        if (!validation.Ok)
        {
            return new(validation.Error);
        }

        if (input.MealGroupId is { } gid && !await groups.IsActiveAsync(gid, ct))
        {
            return new(MealValidationError.UnknownMealGroup);
        }

        entry.Name = input.Name.Trim();
        entry.EatenAt = input.EatenAtUtc;
        entry.PortionSize = input.PortionSize;
        entry.PortionGrams = input.PortionGrams;
        entry.MealGroupId = input.MealGroupId;
        entry.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        entry.UpdatedAt = now;

        entry.EntryTags.Clear();
        await AttachTagsAsync(userId, entry, tags, ct);
        await db.SaveChangesAsync(ct);

        return new(MealValidationError.None, entry.Id);
    }

    /// <summary>Permanently deletes an entry the user owns. False when it does not exist / is not theirs.</summary>
    public async Task<bool> DeleteAsync(Guid userId, Guid entryId, CancellationToken ct = default)
    {
        var deleted = await Owned(userId).Where(e => e.Id == entryId).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    private IQueryable<MealEntry> Owned(Guid userId) => db.MealEntries.Where(e => e.UserId == userId);

    private async Task AttachTagsAsync(Guid userId, MealEntry entry, List<string> tags, CancellationToken ct)
    {
        if (tags.Count == 0)
        {
            return;
        }

        var existing = await db.Tags
            .Where(t => t.UserId == userId && tags.Contains(t.Name))
            .ToDictionaryAsync(t => t.Name, ct);

        foreach (var name in tags)
        {
            if (!existing.TryGetValue(name, out var tag))
            {
                tag = new Tag { UserId = userId, Name = name };
                db.Tags.Add(tag);
                existing[name] = tag;
            }

            entry.EntryTags.Add(new MealEntryTag { MealEntry = entry, Tag = tag });
        }
    }

    internal static MealEntryView ToView(MealEntry e) => new(
        e.Id, e.Name, e.EatenAt, e.PortionSize, e.PortionGrams,
        e.MealGroupId, e.MealGroup?.Key, e.Notes,
        e.EntryTags.Select(t => t.Tag.Name).OrderBy(n => n, StringComparer.Ordinal).ToList());
}
