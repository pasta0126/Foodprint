using Foodprint.Core;
using Foodprint.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Foodprint.Cli;

/// <summary>
/// Admin CLI for Foodprint. Operates directly on the same SQLite database as the web app.
///
///   invite create &lt;email&gt; [--expires yyyy-MM-dd]   invite list   invite revoke &lt;id&gt;
///   user disable &lt;email&gt;   user enable &lt;email&gt;
///   mealgroup add &lt;key&gt;   mealgroup retire &lt;key&gt;
///   db migrate   db admin-link
/// </summary>
public static class CliApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables("FOODPRINT_")
            .AddCommandLine(args)
            .Build();

        var connectionString = config.GetConnectionString("Default")
            ?? config["ConnectionStrings:Default"]
            ?? "Data Source=foodprint.db";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFoodprintCore(connectionString, config);
        await using var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        try
        {
            return await Commands.DispatchAsync(scope.ServiceProvider, args);
        }
        catch (CliError e)
        {
            Console.Error.WriteLine($"error: {e.Message}");
            return 1;
        }
    }
}

public sealed class CliError(string message) : Exception(message);
