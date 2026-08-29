using Foodprint.Core;
using Foodprint.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Foodprint.Tests.Infrastructure;

/// <summary>A fresh, migrated in-memory SQLite database per test, plus a controllable clock.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        Factory = new PooledFactory(options);
        using var db = Factory.CreateDbContext();
        db.Database.Migrate();
    }

    public IDbContextFactory<AppDbContext> Factory { get; }

    public MutableClock Clock { get; } = new(new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero));

    public AppDbContext NewContext() => Factory.CreateDbContext();

    public IOptions<FoodprintOptions> Options(Action<FoodprintOptions>? configure = null)
    {
        var o = new FoodprintOptions
        {
            AdminEmail = "admin@example.com",
            PublicBaseUrl = "http://localhost",
            DefaultTimeZone = "Europe/Madrid",
            RegistrationLinkExpiryDays = 30,
        };
        configure?.Invoke(o);
        return Microsoft.Extensions.Options.Options.Create(o);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class PooledFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}

public sealed class MutableClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
