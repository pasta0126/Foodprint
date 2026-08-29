using Microsoft.Playwright;

namespace Foodprint.Tests.E2E;

[Collection(nameof(AppServerCollection))]
public sealed class MealJourneyE2E(AppServerFixture app) : IAsyncLifetime
{
    private IPlaywright _pw = default!;
    private IBrowser _browser = default!;

    public async Task InitializeAsync()
    {
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _pw.Dispose();
    }

    [Fact]
    public async Task Activate_log_edit_see_everywhere_reauth_then_delete()
    {
        var context = await _browser.NewContextAsync(new() { BaseURL = app.BaseUrl, Locale = "en-US" });
        var page = await context.NewPageAsync();

        // 1. Operator issues an invite link; the user activates it (name + password).
        var activationUrl = await app.CreateInviteAsync("alex@example.com");
        await page.GotoAsync(activationUrl);
        await page.FillAsync("#name", "Alex");
        await page.FillAsync("#password", "correcthorse9");
        await page.ClickAsync("main button[type=submit]");
        await page.WaitForURLAsync(app.BaseUrl + "/");

        // 2. Change language to Catalan and back to English from the profile page.
        await page.GotoAsync("/profile");
        await page.SelectOptionAsync("#lang", "ca");
        await page.ClickAsync("main button[type=submit]");
        await page.WaitForURLAsync("**/profile");
        await page.SelectOptionAsync("#lang", "en");
        await page.ClickAsync("main button[type=submit]");
        await page.WaitForURLAsync("**/profile");

        // 3. Log an entry: grams portion + a meal group + tags.
        await page.GotoAsync("/entries/new");
        await page.FillAsync("#name", "Greek yogurt");
        await page.CheckAsync("input[value=Grams]");
        await page.FillAsync("#portion-grams", "250");
        await page.SelectOptionAsync("#group", new SelectOptionValue { Label = "Breakfast" });
        await page.FillAsync("#tags", "breakfast, quick");
        await page.ClickAsync("main button[type=submit]");
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await Assertions.Expect(page.GetByText("Greek yogurt")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("250 g")).ToBeVisibleAsync();

        // 4. Edit it.
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).First.ClickAsync();        await page.WaitForSelectorAsync("#name");
        await page.FillAsync("#name", "Greek yogurt with honey");
        await page.ClickAsync("main button[type=submit]");
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await Assertions.Expect(page.GetByText("Greek yogurt with honey")).ToBeVisibleAsync();

        // 5. It appears in history and the weekly summary.
        await page.GotoAsync("/history");
        await Assertions.Expect(page.GetByText("Greek yogurt with honey")).ToBeVisibleAsync();
        await page.GotoAsync("/summary");
        await Assertions.Expect(page.GetByText("Current streak")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("#breakfast")).ToBeVisibleAsync();

        // 6. Sign out, then sign back in with email + password.
        await page.GotoAsync("/");
        await page.ClickAsync("text=Sign out");
        await page.WaitForURLAsync("**/sign-in");
        await page.FillAsync("#email", "alex@example.com");
        await page.FillAsync("#password", "correcthorse9");
        await page.ClickAsync("main button[type=submit]");
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await Assertions.Expect(page.GetByText("Greek yogurt with honey")).ToBeVisibleAsync();

        // 7. Delete it via the confirmation dialog.
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).First.ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await page.WaitForSelectorAsync("dialog[open]");
        await page.Locator("dialog button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await Assertions.Expect(page.GetByText("Greek yogurt with honey")).Not.ToBeVisibleAsync();
    }
}
