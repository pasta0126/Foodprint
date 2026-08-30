using System.Net;
using Foodprint.Core.Auth;
using Foodprint.Core.Data;
using Foodprint.Core.Domain;
using Foodprint.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Foodprint.Tests.Export;

[Collection(nameof(WebPipelineCollection))]
public class ExportEndpointTests(FoodprintWebFactory factory)
{
    [Fact]
    public async Task Unauthenticated_request_returns_no_file()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/profile/export");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Authenticated_request_downloads_a_markdown_file_with_only_the_callers_meals()
    {
        var (mineCookie, _) = await SeedUserWithMeal("export-mine@example.com", "My private lunch");
        await SeedUserWithMeal("export-other@example.com", "Someone else's dinner");

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", mineCookie);

        var response = await client.GetAsync("/profile/export?from=2020-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.EndsWith(".md", response.Content.Headers.ContentDisposition.FileName!.Trim('"'));
        Assert.Equal("text/markdown", response.Content.Headers.ContentType!.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Foodprint", body);
        Assert.Contains("My private lunch", body);
        Assert.DoesNotContain("Someone else's dinner", body);
    }

    private async Task<(string Cookie, Guid UserId)> SeedUserWithMeal(string email, string mealName)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var reg = sp.GetRequiredService<RegistrationService>();
        var auth = sp.GetRequiredService<AuthService>();
        var db = sp.GetRequiredService<AppDbContext>();

        var token = await reg.CreateLinkAsync(email, byAdmin: true);
        await reg.ActivateAsync(token!, "Tester", "correcthorse9", "en");

        var userId = (await db.Users.SingleAsync(u => u.Email == email)).Id;
        db.MealEntries.Add(new MealEntry
        {
            UserId = userId,
            Name = mealName,
            EatenAt = DateTime.UtcNow.AddDays(-2),
            PortionSize = "small",
        });
        await db.SaveChangesAsync();

        var session = await auth.CreateSessionAsync(userId);
        return ($"fp_session={session}", userId);
    }
}
