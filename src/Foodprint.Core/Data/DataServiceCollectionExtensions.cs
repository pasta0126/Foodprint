using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Foodprint.Core.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AppDbContext"/> both as a scoped context and as an
    /// <see cref="IDbContextFactory{TContext}"/> (used by interactive Blazor Server
    /// components, whose lifetime outlives a request). SQLite runs in WAL mode.
    /// </summary>
    public static IServiceCollection AddFoodprintData(this IServiceCollection services, string connectionString)
    {
        EnableWal(connectionString);

        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<AppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        return services;
    }

    private static void EnableWal(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (builder.DataSource is ":memory:" || string.IsNullOrEmpty(builder.DataSource))
        {
            return;
        }

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();
    }
}
