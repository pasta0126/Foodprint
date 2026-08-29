using Foodprint.Core;
using Foodprint.Core.Data;
using Foodprint.Web.Auth;
using Foodprint.Web.Components;
using Foodprint.Web.Components.Meals;
using Foodprint.Web.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=foodprint.db";

var options = builder.Configuration.GetSection(FoodprintOptions.SectionName).Get<FoodprintOptions>()
    ?? new FoodprintOptions();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddFoodprintCore(connectionString, builder.Configuration);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(Directory.CreateDirectory(options.DataProtectionKeyPath));

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opts.KnownIPNetworks.Clear();
    opts.KnownProxies.Clear();

    var networks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
    if (networks.Length > 0)
    {
        foreach (var network in networks)
        {
            opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    }
    else
    {
        // No explicit proxy network configured: trust any hop. Safe here because the
        // container is only reachable through the reverse proxy, never directly.
        opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("0.0.0.0/0"));
        opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("::/0"));
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<Foodprint.Web.Localization.ILanguagePersistence, Foodprint.Web.Localization.ProfileLanguagePersistence>();

builder.Services.AddAuthentication(SessionAuth.Scheme)
    .AddScheme<SessionAuthOptions, SessionAuthHandler>(SessionAuth.Scheme, _ => { });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(SessionAuth.AdminPolicy, p => p.RequireRole("admin"));

builder.Services.AddLocalization();
builder.Services.AddSingleton(FoodprintRequestLocalization.Build());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    app.UseForwardedHeaders();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<Foodprint.Core.Auth.AdminBootstrapper>();
    var link = await bootstrapper.EnsureAsync();
    if (link is not null)
    {
        app.Logger.LogWarning("Admin activation link (open once to set the password): {Url}",
            bootstrapper.ActivationUrl(link));
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// TLS is terminated at the reverse proxy (void-server); the app relies on forwarded
// headers, HSTS and Secure cookies rather than its own HTTPS redirect.

app.UseAuthentication();
app.UseRequestLocalization(app.Services.GetRequiredService<RequestLocalizationOptions>());
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapLanguageEndpoints();
app.MapAuthEndpoints();
app.MapMealEndpoints();

app.Run();

/// <summary>Exposed so the test host (WebApplicationFactory) can reference the entry assembly.</summary>
public partial class Program;
