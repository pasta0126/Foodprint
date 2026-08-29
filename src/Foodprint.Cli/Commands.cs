using Foodprint.Core;
using Foodprint.Core.Auth;
using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Foodprint.Cli;

internal static class Commands
{
    public static async Task<int> DispatchAsync(IServiceProvider sp, string[] args)
    {
        var pos = args.Where(a => !a.StartsWith('-')).ToArray();
        var flags = args.Where(a => a.StartsWith("--")).ToArray();
        string? Flag(string name) => flags
            .FirstOrDefault(f => f.StartsWith($"--{name}=", StringComparison.Ordinal))?
            .Split('=', 2)[1];

        switch (pos)
        {
            case ["db", "migrate"]:
                await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();
                Console.WriteLine("Database migrated. Meal-group catalog seeded.");
                return 0;

            case ["db", "admin-link"]:
            {
                var boot = sp.GetRequiredService<AdminBootstrapper>();
                var raw = await boot.EnsureAsync();
                Console.WriteLine(raw is null
                    ? "Admin account already has a password. Use 'invite create <admin-email>' to issue a reset link."
                    : boot.ActivationUrl(raw));
                return 0;
            }

            case ["invite", "create", var email]:
            {
                DateTime? expires = Flag("expires") is { } s ? DateTime.Parse(s).ToUniversalTime() : null;
                var raw = await sp.GetRequiredService<RegistrationService>()
                    .CreateLinkAsync(email, byAdmin: true, expiresAt: expires);
                if (raw is null)
                {
                    throw new CliError("could not create a link for that address");
                }

                var baseUrl = sp.GetRequiredService<IOptions<FoodprintOptions>>().Value.PublicBaseUrl.TrimEnd('/');
                Console.WriteLine($"{baseUrl}/activate/{raw}");
                return 0;
            }

            case ["invite", "list"]:
            {
                var links = await sp.GetRequiredService<RegistrationService>().ListAsync();
                foreach (var l in links)
                {
                    var state = l.RevokedAt is not null ? "revoked"
                        : l.UsedAt is not null ? "used"
                        : l.ExpiresAt <= DateTime.UtcNow ? "expired"
                        : "active";
                    Console.WriteLine($"{l.Id}  {l.Email,-32}  {state,-8}  expires {l.ExpiresAt:yyyy-MM-dd}");
                }

                return 0;
            }

            case ["invite", "revoke", var id]:
                return await sp.GetRequiredService<RegistrationService>().RevokeAsync(Guid.Parse(id))
                    ? Ok("revoked") : Fail("link not found or already revoked");

            case ["user", "disable", var email]:
                return await sp.GetRequiredService<AuthService>().SetDisabledAsync(email, true)
                    ? Ok($"{email} disabled; sessions cleared") : Fail("user not found");

            case ["user", "enable", var email]:
                return await sp.GetRequiredService<AuthService>().SetDisabledAsync(email, false)
                    ? Ok($"{email} enabled") : Fail("user not found");

            case ["mealgroup", "add", var key]:
                return await AddMealGroup(sp, key);

            case ["mealgroup", "retire", var key]:
                return await RetireMealGroup(sp, key);

            default:
                PrintUsage();
                return pos.Length == 0 ? 0 : 1;
        }
    }

    private static async Task<int> AddMealGroup(IServiceProvider sp, string key)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        key = key.Trim().ToLowerInvariant();
        if (await db.MealGroups.AnyAsync(g => g.Key == key))
        {
            return Fail("a meal group with that key already exists");
        }

        var max = await db.MealGroups.MaxAsync(g => (int?)g.SortOrder) ?? 0;
        db.MealGroups.Add(new MealGroup { Key = key, SortOrder = max + 10 });
        await db.SaveChangesAsync();
        return Ok($"added meal group '{key}'");
    }

    private static async Task<int> RetireMealGroup(IServiceProvider sp, string key)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var group = await db.MealGroups.FirstOrDefaultAsync(g => g.Key == key.Trim().ToLowerInvariant());
        if (group is null)
        {
            return Fail("meal group not found");
        }

        group.RetiredAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok($"retired meal group '{key}'");
    }

    private static int Ok(string message)
    {
        Console.WriteLine(message);
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        return 1;
    }

    private static void PrintUsage() => Console.WriteLine(
        """
        Foodprint admin CLI

          invite create <email> [--expires=yyyy-MM-dd]
          invite list
          invite revoke <id>
          user disable <email>
          user enable <email>
          mealgroup add <key>
          mealgroup retire <key>
          db migrate
          db admin-link
        """);
}
