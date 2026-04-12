using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;

namespace EnterpriseAgentOs.Api.Tests.LlmProxy;

/// <summary>
/// Verifies that configuring API keys for non-OpenAI providers returns 400
/// and that OpenAI still accepts a key via PUT /api/providers/openai.
/// </summary>
[Collection("Integration")]
public sealed class ProvidersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProvidersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("anthropic")]
    [InlineData("google")]
    [InlineData("xai")]
    public async Task Configure_NonOpenAiProvider_Returns400(string providerName)
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/providers/{providerName}",
            new { apiKey = "sk-test-key" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("OpenAI", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Configure_OpenAiProvider_Returns200()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/providers/openai",
            new { apiKey = "sk-test-openai-key" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_Providers_NonOpenAi_ShowConfiguredTrue()
    {
        var response = await _client.GetAsync("/api/providers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var providers = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(providers);

        // anthropic, google, xai should all be configured = true (platform keys)
        foreach (var name in new[] { "anthropic", "google", "xai" })
        {
            var provider = providers.FirstOrDefault(p =>
                p.GetProperty("name").GetString()
                    ?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

            Assert.True(provider.ValueKind != JsonValueKind.Undefined,
                $"Provider '{name}' not found in list");
            Assert.True(provider.GetProperty("configured").GetBoolean(),
                $"Provider '{name}' should be configured=true");
        }
    }
}
