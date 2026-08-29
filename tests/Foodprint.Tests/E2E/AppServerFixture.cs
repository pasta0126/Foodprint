using System.Diagnostics;
using System.Text;
using Foodprint.Cli;

namespace Foodprint.Tests.E2E;

/// <summary>
/// Starts the real Foodprint.Web app as a Kestrel subprocess against a throwaway
/// SQLite database, and lets tests mint activation links through the admin CLI —
/// exactly the operator flow.
/// </summary>
public sealed class AppServerFixture : IAsyncLifetime
{
    private readonly string _workDir = Path.Combine(Path.GetTempPath(), $"fp-e2e-{Guid.NewGuid():N}");
    private Process? _process;

    public int Port { get; } = FreePort();
    public string BaseUrl => $"http://localhost:{Port}";
    public string DbConnectionString => $"Data Source={Path.Combine(_workDir, "foodprint.db")}";

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_workDir);

        var dll = Path.Combine(RepoRoot(), "src", "Foodprint.Web", "bin", "Debug", "net10.0", "Foodprint.Web.dll");
        var psi = new ProcessStartInfo("dotnet", $"exec \"{dll}\"")
        {
            WorkingDirectory = _workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["ASPNETCORE_URLS"] = BaseUrl;
        psi.Environment["FOODPRINT_ConnectionStrings__Default"] = DbConnectionString;
        psi.Environment["FOODPRINT_Foodprint__PublicBaseUrl"] = BaseUrl;
        psi.Environment["FOODPRINT_Foodprint__AllowSelfRegistration"] = "false";
        psi.Environment["FOODPRINT_Foodprint__DataProtectionKeyPath"] = Path.Combine(_workDir, "dp-keys");
        psi.Environment["FOODPRINT_Foodprint__AdminEmail"] = "admin@example.com";

        _process = Process.Start(psi)!;
        var ready = new TaskCompletionSource();
        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            if (e.Data.Contains("Now listening on"))
            {
                ready.TrySetResult();
            }
        };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await Task.WhenAny(ready.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        if (!ready.Task.IsCompleted)
        {
            throw new InvalidOperationException("Foodprint.Web did not start within 30s.");
        }
    }

    /// <summary>Runs `invite create &lt;email&gt;` through the CLI and returns the activation URL.</summary>
    public async Task<string> CreateInviteAsync(string email)
    {
        Environment.SetEnvironmentVariable("FOODPRINT_ConnectionStrings__Default", DbConnectionString);
        Environment.SetEnvironmentVariable("FOODPRINT_Foodprint__PublicBaseUrl", BaseUrl);

        var original = Console.Out;
        var buffer = new StringWriter();
        Console.SetOut(buffer);
        try
        {
            var code = await CliApp.RunAsync(["invite", "create", email]);
            if (code != 0)
            {
                throw new InvalidOperationException($"invite create failed: {buffer}");
            }
        }
        finally
        {
            Console.SetOut(original);
        }

        return buffer.ToString().Trim();
    }

    public Task DisposeAsync()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            Directory.Delete(_workDir, recursive: true);
        }
        catch
        {
            // ignore
        }

        return Task.CompletedTask;
    }

    private static int FreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Foodprint.slnx")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException("Repo root not found.");
    }
}

[CollectionDefinition(nameof(AppServerCollection))]
public sealed class AppServerCollection : ICollectionFixture<AppServerFixture>;
