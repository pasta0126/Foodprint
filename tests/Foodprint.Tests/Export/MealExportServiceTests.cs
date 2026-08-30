using Foodprint.Core.Auth;
using Foodprint.Core.Domain;
using Foodprint.Core.Export;
using Foodprint.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Tests.Export;

public class MealExportServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly IPasswordHasher _hasher = new Argon2PasswordHasher();
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    // The test clock is fixed at 2026-08-30 12:00 UTC.
    private static readonly DateOnly Today = new(2026, 8, 30);

    private MealExportService Export() => new(_db.NewContext(), _db.Clock);

    private async Task<Guid> NewUser(string email)
    {
        var reg = new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options());
        var token = await reg.CreateLinkAsync(email, byAdmin: true);
        await new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options())
            .ActivateAsync(token!, "U", "correcthorse1", "es");
        await using var db = _db.NewContext();
        return db.Users.Single(u => u.Email == email).Id;
    }

    private async Task Add(Guid userId, DateTime eatenAtUtc, string name, int? groupId = null, string? size = "small", params string[] tags)
    {
        await using var db = _db.NewContext();
        var entry = new MealEntry { UserId = userId, Name = name, EatenAt = eatenAtUtc, PortionSize = size, MealGroupId = groupId };
        foreach (var t in tags)
        {
            var tag = db.Tags.SingleOrDefault(x => x.UserId == userId && x.Name == t) ?? new Tag { UserId = userId, Name = t };
            entry.EntryTags.Add(new MealEntryTag { MealEntry = entry, Tag = tag });
        }

        db.MealEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    private Task<MealExport> Build(Guid user, DateOnly? from, DateOnly? to) =>
        Export().BuildAsync(user, from, to, Madrid, "English", ExportStringsFixture.English);

    [Fact]
    public async Task Explicit_range_includes_only_that_local_window()
    {
        var user = await NewUser("a@example.com");
        await Add(user, new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc), "before");
        await Add(user, new DateTime(2026, 8, 1, 6, 0, 0, DateTimeKind.Utc), "inside-early");
        await Add(user, new DateTime(2026, 8, 15, 20, 0, 0, DateTimeKind.Utc), "inside-late");
        await Add(user, new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc), "after");

        var md = (await Build(user, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 15))).Markdown;

        Assert.Contains("inside-early", md);
        Assert.Contains("inside-late", md);
        Assert.DoesNotContain("| 2026-07-31", md);
        Assert.DoesNotContain("after", md);
        Assert.Contains("Total meals: 2", md);
    }

    [Fact]
    public async Task Open_ended_range_covers_all_history_through_today()
    {
        var user = await NewUser("b@example.com");
        await Add(user, new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), "old");
        await Add(user, new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc), "recent");

        var export = await Build(user, null, null);

        Assert.StartsWith("foodprint-all-2026-08-30.md", export.FileName);
        Assert.Contains("old", export.Markdown);
        Assert.Contains("recent", export.Markdown);
        Assert.Contains("first entry", export.Markdown);
    }

    [Fact]
    public async Task Reversed_range_falls_back_to_full_history()
    {
        var user = await NewUser("c@example.com");
        await Add(user, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), "x");

        var export = await Build(user, new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 1));

        Assert.StartsWith("foodprint-all-2026-08-30.md", export.FileName);
        Assert.Contains("x", export.Markdown);
    }

    [Fact]
    public async Task Aggregates_and_missing_days_are_computed()
    {
        var user = await NewUser("d@example.com");
        const int lunch = 2; // seeded catalog id
        // 4 distinct days, 10 entries total, 6 of them tagged "lunch".
        var perDay = new (int Day, int Count, int Tagged)[] { (1, 2, 2), (3, 4, 2), (5, 2, 2), (7, 2, 0) };
        foreach (var (day, count, tagged) in perDay)
        {
            for (var i = 0; i < count; i++)
            {
                var tags = i < tagged ? new[] { "lunch" } : [];
                await Add(user, new DateTime(2026, 8, day, 10 + i, 0, 0, DateTimeKind.Utc), $"m{day}-{i}", lunch, "small", tags);
            }
        }

        var md = (await Build(user, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 7))).Markdown;

        Assert.Contains("Total meals: 10", md);
        Assert.Contains("Days with entries: 4", md);
        Assert.Contains("| #lunch | 6 |", md);
        Assert.Contains("| Lunch | 10 |", md);
        Assert.Contains("## Days with no entry", md);
        Assert.Contains("- 2026-08-02", md);
        Assert.Contains("- 2026-08-06", md);
    }

    [Fact]
    public async Task Only_the_owners_entries_are_exported()
    {
        var alice = await NewUser("alice@example.com");
        var bob = await NewUser("bob@example.com");
        await Add(alice, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), "alice meal");
        await Add(bob, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), "bob meal");

        var md = (await Build(alice, null, null)).Markdown;

        Assert.Contains("alice meal", md);
        Assert.DoesNotContain("bob meal", md);
        Assert.Contains("Total meals: 1", md);
    }

    [Fact]
    public async Task Empty_range_yields_header_legend_and_no_entries_note()
    {
        var user = await NewUser("e@example.com");
        var md = (await Build(user, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 7))).Markdown;

        Assert.Contains("# Foodprint meal log", md);
        Assert.Contains("## Legend", md);
        Assert.Contains("Total meals: 0", md);
        Assert.Contains("_No entries in this range._", md);
    }

    public void Dispose() => _db.Dispose();
}
