using Foodprint.Tests.Infrastructure;

namespace Foodprint.Tests.Localization;

public class LocalizationPipelineTests(FoodprintWebFactory factory) : IClassFixture<FoodprintWebFactory>
{
    [Theory]
    [InlineData("ca", "ca")]
    [InlineData("en", "en")]
    [InlineData("fr", "es")]   // unsupported -> Spanish fallback
    [InlineData("", "es")]     // no preference -> Spanish
    public async Task Anonymous_visitor_language_follows_accept_language(string accept, string expected)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (!string.IsNullOrEmpty(accept))
        {
            request.Headers.Add("Accept-Language", accept);
        }

        var response = await client.SendAsync(request);
        var contentLanguage = response.Content.Headers.ContentLanguage.SingleOrDefault();

        Assert.Equal(expected, contentLanguage);
    }
}
