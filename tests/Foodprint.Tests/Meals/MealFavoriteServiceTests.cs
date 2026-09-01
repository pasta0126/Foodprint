using Foodprint.Core.Auth;
using Foodprint.Core.Meals;
using Foodprint.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Tests.Meals;

public class MealFavoriteServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly IPasswordHasher _hasher = new Argon2PasswordHasher();

    private MealFavoriteService Favorites() => new(_db.NewContext(), _db.Clock);

    private async Task<Guid> NewUser(string email)
    {
        var reg = new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options());
        var token = await reg.CreateLinkAsync(email, byAdmin: true);
        await new RegistrationService(_db.NewContext(), _hasher, _db.Clock, _db.Options())
            .ActivateAsync(token!, "U", "correcthorse1", "es");
        await using var db = _db.NewContext();
        return db.Users.Single(u => u.Email == email).Id;
    }

    private static FavoriteDraft Draft(string name, string? size = "small", int? group = 1, params string[] tags) => new()
    {
        Name = name,
        PortionSize = size,
        MealGroupId = group,
        Tags = tags,
    };

    [Fact]
    public async Task Save_creates_a_favorite()
    {
        var user = await NewUser("a@example.com");

        var fav = await Favorites().SaveAsync(user, Draft("Greek yogurt", "small", 1, "quick"));

        var view = await Favorites().GetAsync(user, fav.Id);
        Assert.Equal("Greek yogurt", view!.Name);
        Assert.Equal("small", view.PortionSize);
        Assert.Equal(1, view.MealGroupId);
        Assert.Equal(["quick"], view.Tags);
    }

    [Fact]
    public async Task Save_and_re_save_carry_notes()
    {
        var user = await NewUser("notes@example.com");
        var draft = Draft("Greek yogurt", "small", 1);
        draft.Notes = "with honey";

        var fav = await Favorites().SaveAsync(user, draft);

        var view = await Favorites().GetAsync(user, fav.Id);
        Assert.Equal("with honey", view!.Notes);

        var updated = Draft("Greek yogurt", "small", 1);
        updated.Notes = "no honey";
        await Favorites().SaveAsync(user, updated);

        var reView = await Favorites().GetAsync(user, fav.Id);
        Assert.Equal("no honey", reView!.Notes);
    }

    [Fact]
    public async Task Re_saving_same_name_and_group_updates_in_place()
    {
        var user = await NewUser("b@example.com");
        await Favorites().SaveAsync(user, Draft("Greek yogurt", "small", 1));

        await Favorites().SaveAsync(user, Draft("  greek YOGURT ", "medium", 1, "protein"));

        await using var db = _db.NewContext();
        var all = db.MealFavorites.Where(f => f.UserId == user).ToList();
        Assert.Single(all);
        Assert.Equal("medium", all[0].PortionSize);
        Assert.Equal("greek yogurt", all[0].NameNormalized);
        Assert.Equal("protein", all[0].TagsCsv);
    }

    [Fact]
    public async Task Same_name_different_meal_group_is_a_distinct_favorite()
    {
        var user = await NewUser("c@example.com");
        await Favorites().SaveAsync(user, Draft("Toast", "small", 1));
        await Favorites().SaveAsync(user, Draft("Toast", "small", 4));

        await using var db = _db.NewContext();
        Assert.Equal(2, db.MealFavorites.Count(f => f.UserId == user));
    }

    [Fact]
    public async Task ListGrouped_orders_by_meal_group_then_no_group_last()
    {
        var user = await NewUser("d@example.com");
        await Favorites().SaveAsync(user, Draft("No group", "small", null));
        await Favorites().SaveAsync(user, Draft("Dinner thing", "small", 3));
        await Favorites().SaveAsync(user, Draft("Breakfast thing", "small", 1));

        var groups = await Favorites().ListGroupedAsync(user);

        Assert.Equal(["breakfast", "dinner", null], groups.Select(g => g.Key));
        Assert.Equal("Breakfast thing", groups[0].Favorites.Single().Name);
    }

    [Fact]
    public async Task Get_and_delete_are_scoped_to_the_owner()
    {
        var alice = await NewUser("alice@example.com");
        var bob = await NewUser("bob@example.com");
        var fav = await Favorites().SaveAsync(alice, Draft("Mine"));

        Assert.Null(await Favorites().GetAsync(bob, fav.Id));
        Assert.False(await Favorites().DeleteAsync(bob, fav.Id));
        Assert.NotNull(await Favorites().GetAsync(alice, fav.Id));
        Assert.True(await Favorites().DeleteAsync(alice, fav.Id));
        Assert.Null(await Favorites().GetAsync(alice, fav.Id));
    }

    public void Dispose() => _db.Dispose();
}
