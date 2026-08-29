using System.Net;
using Foodprint.Tests.Infrastructure;

namespace Foodprint.Tests.Auth;

public class AuthPipelineTests(FoodprintWebFactory factory) : IClassFixture<FoodprintWebFactory>
{
    [Fact]
    public async Task Unauthenticated_page_request_redirects_to_sign_in()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/sign-in", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Unauthenticated_json_request_gets_401()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Accept.ParseAdd("application/json");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sign_in_page_is_reachable_anonymously()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/sign-in");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Invalid_activation_token_shows_neutral_page()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/activate/not-a-real-token");
        request.Headers.Add("Accept-Language", "en");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("no longer valid", body, StringComparison.OrdinalIgnoreCase);
    }
}
