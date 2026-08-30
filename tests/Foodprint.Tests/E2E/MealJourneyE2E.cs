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
        await page.Locator("form:has(#lang) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync("**/profile");
        await page.SelectOptionAsync("#lang", "en");
        await page.Locator("form:has(#lang) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync("**/profile");
        // The profile page is the account hub: theme control + sign-out live here.
        await Assertions.Expect(page.Locator("#fp-theme-control")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("main form[action='/auth/sign-out']")).ToBeVisibleAsync();

        // 3. Log an entry from the home form: grams portion + meal group + tags, and save it as a favorite.
        await page.GotoAsync("/");
        await page.FillAsync("#name", "Greek yogurt");
        // The meal group is pre-selected from the time of day; it can be overridden.
        Assert.NotEqual("", await page.Locator("#group").InputValueAsync());
        await page.GetByText("I'd rather enter exact grams").ClickAsync();
        await page.FillAsync("#portion-grams", "250");
        await page.SelectOptionAsync("#group", new SelectOptionValue { Label = "Breakfast" });
        await page.FillAsync("#tags", "breakfast, quick");
        await page.GetByLabel("Save to favorites").CheckAsync();
        await page.Locator("#log button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(app.BaseUrl + "/");

        // 4. It shows in history, grouped under today, with its grams portion.
        await page.GotoAsync("/history");
        await Assertions.Expect(page.GetByText("Greek yogurt")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("250 g")).ToBeVisibleAsync();

        // 5. Edit it from history.
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).First.ClickAsync();
        await page.WaitForSelectorAsync("#name");
        await page.FillAsync("#name", "Greek yogurt with honey");
        await page.Locator("main button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await page.GotoAsync("/history");
        await Assertions.Expect(page.GetByText("Greek yogurt with honey")).ToBeVisibleAsync();

        // 6. The weekly summary lives on the home; the old routes redirect there.
        await page.GotoAsync("/summary");
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await Assertions.Expect(page.GetByText("Current streak")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".fp-taglist").GetByText("#breakfast")).ToBeVisibleAsync();
        await page.GotoAsync("/entries/new");
        await page.WaitForURLAsync(app.BaseUrl + "/");

        // 7. Use the saved favorite card to pre-fill and log a second entry.
        await page.Locator(".fp-quickcard__pick").First.ClickAsync();
        await Assertions.Expect(page.Locator("#name")).ToHaveValueAsync("Greek yogurt");
        await page.Locator("#log button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await page.GotoAsync("/history");
        await Assertions.Expect(page.GetByText("Greek yogurt", new() { Exact = true })).ToBeVisibleAsync();

        // 8. Delete the favorite from its card.
        await page.GotoAsync("/");
        await page.Locator(".fp-quickcard__del button").First.ClickAsync();
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await Assertions.Expect(page.Locator(".fp-quickcard")).ToHaveCountAsync(0);

        // 9. Sign out (from the profile page), then sign back in with email + password.
        await page.GotoAsync("/profile");
        await page.Locator("main form[action='/auth/sign-out'] button[type=submit]").ClickAsync();
        await page.WaitForURLAsync("**/sign-in");
        await page.FillAsync("#email", "alex@example.com");
        await page.FillAsync("#password", "correcthorse9");
        await page.ClickAsync("main button[type=submit]");
        await page.WaitForURLAsync(app.BaseUrl + "/");

        // 10. Export the meal log from the profile page.
        await page.GotoAsync("/profile");
        await Assertions.Expect(page.Locator("#export-from")).ToBeVisibleAsync();
        var export = await page.APIRequest.GetAsync(app.BaseUrl + "/profile/export?from=2020-01-01");
        Assert.Equal(200, export.Status);
        Assert.Contains("text/markdown", export.Headers["content-type"]);
        Assert.Contains("attachment", export.Headers["content-disposition"]);
        var exportBody = await export.TextAsync();
        Assert.Contains("Foodprint meal log", exportBody);
        Assert.Contains("Greek yogurt with honey", exportBody);

        // 11. Delete an entry via the confirmation dialog.
        await page.GotoAsync("/history");
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).First.ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await page.WaitForSelectorAsync("dialog[open]");
        await page.Locator("dialog button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(app.BaseUrl + "/");
        await page.GotoAsync("/history");
        await Assertions.Expect(page.GetByText("Greek yogurt with honey")).Not.ToBeVisibleAsync();
    }
}
