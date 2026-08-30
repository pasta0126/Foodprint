using Foodprint.Core.Auth;
using Foodprint.Core.Meals;
using Foodprint.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Tests.Meals;

public class MealEntryServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly IPasswordHasher _hasher = new Argon2PasswordHasher();

    private MealEntryService Entries()
    {
        var ctx = _db.NewContext();
        return new MealEntryService(ctx, new MealGroupService(ctx), _db.Clock);
    }

    private async Task<Guid> NewUser(string email)
    {
        var reg = new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options());
        var token = await reg.CreateLinkAsync(email, byAdmin: true);
        await new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options())
            .ActivateAsync(token!, "U", "correcthorse1", "es");
        await using var db = _db.NewContext();
        return db.Users.Single(u => u.Email == email).Id;
    }

    private MealEntryInput Valid(string name = "Toast") => new()
    {
        Name = name,
        EatenAtUtc = _db.Clock.GetUtcNow().UtcDateTime.AddHours(-1),
        PortionSize = "medium", // portion is required; tests that care override it
    };

    [Fact]
    public async Task Minimal_entry_is_created_with_given_time()
    {
        var user = await NewUser("a@example.com");
        var result = await Entries().CreateAsync(user, Valid());

        Assert.True(result.Ok);
        var view = await Entries().GetAsync(user, result.EntryId);
        Assert.Equal("Toast", view!.Name);
        Assert.Equal("medium", view.PortionSize);
        Assert.Empty(view.Tags);
    }

    [Fact]
    public async Task Name_is_required_and_length_capped()
    {
        var user = await NewUser("n@example.com");
        var blank = Valid(); blank.Name = "   ";
        Assert.Equal(MealValidationError.NameRequired, (await Entries().CreateAsync(user, blank)).Error);

        var big = Valid(); big.Name = new string('x', 121);
        Assert.Equal(MealValidationError.NameTooLong, (await Entries().CreateAsync(user, big)).Error);
    }

    [Fact]
    public async Task Eaten_at_more_than_24h_ahead_is_rejected()
    {
        var user = await NewUser("f@example.com");
        var future = Valid();
        future.EatenAtUtc = _db.Clock.GetUtcNow().UtcDateTime.AddHours(25);
        Assert.Equal(MealValidationError.EatenAtTooFarInFuture, (await Entries().CreateAsync(user, future)).Error);

        future.EatenAtUtc = _db.Clock.GetUtcNow().UtcDateTime.AddHours(23);
        Assert.True((await Entries().CreateAsync(user, future)).Ok);
    }

    [Fact]
    public async Task Past_dates_are_unrestricted()
    {
        var user = await NewUser("p@example.com");
        var old = Valid();
        old.EatenAtUtc = _db.Clock.GetUtcNow().UtcDateTime.AddYears(-3);
        Assert.True((await Entries().CreateAsync(user, old)).Ok);
    }

    [Fact]
    public async Task Portion_size_xor_grams()
    {
        var user = await NewUser("port@example.com");

        var none = Valid(); none.PortionSize = null; none.PortionGrams = null;
        Assert.Equal(MealValidationError.PortionRequired, (await Entries().CreateAsync(user, none)).Error);

        var both = Valid(); both.PortionSize = "small"; both.PortionGrams = 100;
        Assert.Equal(MealValidationError.PortionBothProvided, (await Entries().CreateAsync(user, both)).Error);

        var badSize = Valid(); badSize.PortionSize = "huge";
        Assert.Equal(MealValidationError.PortionSizeInvalid, (await Entries().CreateAsync(user, badSize)).Error);

        foreach (var grams in new[] { 0, 5001 })
        {
            var g = Valid(); g.PortionSize = null; g.PortionGrams = grams;
            Assert.Equal(MealValidationError.PortionGramsOutOfRange, (await Entries().CreateAsync(user, g)).Error);
        }

        var okG = Valid(); okG.PortionSize = null; okG.PortionGrams = 250;
        Assert.True((await Entries().CreateAsync(user, okG)).Ok);
    }

    [Theory]
    [InlineData("small")]
    [InlineData("medium")]
    [InlineData("large")]
    [InlineData("very-large")]
    public async Task Named_portion_sizes_are_all_accepted(string size)
    {
        var user = await NewUser($"size-{size}@example.com");
        var input = Valid(); input.PortionSize = size;

        var result = await Entries().CreateAsync(user, input);

        Assert.True(result.Ok);
        var view = await Entries().GetAsync(user, result.EntryId);
        Assert.Equal(size, view!.PortionSize);
    }

    [Fact]
    public async Task Tags_are_normalized_deduped_and_capped()
    {
        var user = await NewUser("t@example.com");
        var input = Valid();
        input.Tags = ["  Lunch ", "lunch", "HOME"];

        var result = await Entries().CreateAsync(user, input);
        var view = await Entries().GetAsync(user, result.EntryId);
        Assert.Equal(new[] { "home", "lunch" }, view!.Tags);

        var many = Valid();
        many.Tags = Enumerable.Range(0, 11).Select(i => $"tag{i}").ToArray();
        Assert.Equal(MealValidationError.TooManyTags, (await Entries().CreateAsync(user, many)).Error);

        var longTag = Valid();
        longTag.Tags = [new string('z', 31)];
        Assert.Equal(MealValidationError.TagTooLong, (await Entries().CreateAsync(user, longTag)).Error);
    }

    [Fact]
    public async Task Tags_are_upserted_per_user_not_duplicated()
    {
        var user = await NewUser("u1@example.com");
        var first = Valid(); first.Tags = ["breakfast"];
        var second = Valid(); second.Tags = ["breakfast", "quick"];
        await Entries().CreateAsync(user, first);
        await Entries().CreateAsync(user, second);

        await using var db = _db.NewContext();
        Assert.Equal(2, db.Tags.Count(t => t.UserId == user));
    }

    [Fact]
    public async Task Meal_group_must_be_active()
    {
        var user = await NewUser("g@example.com");
        await using (var db = _db.NewContext())
        {
            var lunch = db.MealGroups.Single(g => g.Key == "lunch");
            var other = db.MealGroups.Single(g => g.Key == "other");
            other.RetiredAt = _db.Clock.GetUtcNow().UtcDateTime;
            db.SaveChanges();

            var ok = Valid(); ok.MealGroupId = lunch.Id;
            Assert.True((await Entries().CreateAsync(user, ok)).Ok);

            var retired = Valid(); retired.MealGroupId = other.Id;
            Assert.Equal(MealValidationError.UnknownMealGroup, (await Entries().CreateAsync(user, retired)).Error);

            var missing = Valid(); missing.MealGroupId = 9999;
            Assert.Equal(MealValidationError.UnknownMealGroup, (await Entries().CreateAsync(user, missing)).Error);
        }
    }

    [Fact]
    public async Task Update_sets_updated_at_and_replaces_tags()
    {
        var user = await NewUser("upd@example.com");
        var created = await Entries().CreateAsync(user, Valid());
        var id = created.EntryId;

        _db.Clock.Advance(TimeSpan.FromMinutes(5));
        var edit = Valid("Porridge");
        edit.Tags = ["warm"];
        var result = await Entries().UpdateAsync(user, id, edit);
        Assert.True(result.Ok);

        var view = await Entries().GetAsync(user, id);
        Assert.Equal("Porridge", view!.Name);
        Assert.Equal(new[] { "warm" }, view.Tags);

        await using var db = _db.NewContext();
        var row = db.MealEntries.Single(e => e.Id == id);
        Assert.True(row.UpdatedAt > row.CreatedAt);
    }

    [Fact]
    public async Task Users_cannot_read_update_or_delete_each_others_entries()
    {
        var alice = await NewUser("alice@example.com");
        var bob = await NewUser("bob@example.com");
        var entry = (await Entries().CreateAsync(alice, Valid())).EntryId;

        Assert.Null(await Entries().GetAsync(bob, entry));
        Assert.True((await Entries().UpdateAsync(bob, entry, Valid("Hack"))).NotFound);
        Assert.False(await Entries().DeleteAsync(bob, entry));

        Assert.NotNull(await Entries().GetAsync(alice, entry));
        Assert.True(await Entries().DeleteAsync(alice, entry));
        Assert.Null(await Entries().GetAsync(alice, entry));
    }

    public void Dispose() => _db.Dispose();
}
