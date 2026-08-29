using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Foodprint.Tests.Infrastructure;

/// <summary>
/// Boots the real Foodprint.Web app against a throwaway SQLite file so integration
/// tests exercise the full middleware pipeline.
/// </summary>
public sealed class FoodprintWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fp-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["Foodprint:DataProtectionKeyPath"] = Path.Combine(Path.GetTempPath(), $"fp-dp-{Guid.NewGuid():N}"),
                ["Foodprint:AdminEmail"] = "admin@example.com",
                ["Foodprint:PublicBaseUrl"] = "http://localhost",
                ["Foodprint:AllowSelfRegistration"] = "true",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_dbPath) + "*"))
        {
            try { File.Delete(f); } catch { /* best effort */ }
        }
    }
}
