namespace EnterpriseAgentOs.Api.Tests.LlmProxy;

/// <summary>
/// Verifies that configuring API keys for non-OpenAI providers returns a GraphQL
/// error and that OpenAI still accepts a key via the setProviderKey mutation.
/// All dashboard provider management is now GraphQL (Stage 6).
/// </summary>
[Collection("Integration")]
public sealed class ProvidersControllerTests : IClassFixture<EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory>
{
    private readonly EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory _factory;

    public ProvidersControllerTests(EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private const string SetProviderKeyMutation = @"
        mutation($providerName: String!, $apiKey: String!) {
          setProviderKey(providerName: $providerName, apiKey: $apiKey) {
            id name configured
          }
        }";

    [Theory]
    [InlineData("anthropic")]
    [InlineData("google")]
    [InlineData("xai")]
    public async Task Configure_NonOpenAiProvider_ReturnsError(string providerName)
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var raw = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLRawAsync(client, SetProviderKeyMutation,
            new { providerName, apiKey = "sk-test-key" });

        Assert.True(raw.TryGetProperty("errors", out var errors));
        Assert.True(errors.GetArrayLength() > 0);
        Assert.Contains("OpenAI", errors.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Configure_OpenAiProvider_Succeeds()
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLAsync(client, SetProviderKeyMutation,
            new { providerName = "openai", apiKey = "sk-test-openai-key" });

        var provider = data.GetProperty("setProviderKey");
        Assert.Equal("openai", provider.GetProperty("name").GetString());
    }

    [Fact]
    public async Task List_Providers_NonOpenAi_ShowConfiguredTrue()
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLAsync(client, "{ providers { name configured } }");
        var providers = data.GetProperty("providers");
        Assert.True(providers.GetArrayLength() > 0);

        // anthropic, google, xai should all be configured = true (platform keys)
        foreach (var name in new[] { "anthropic", "google", "xai" })
        {
            var provider = providers.EnumerateArray().FirstOrDefault(p =>
                p.GetProperty("name").GetString()
                    ?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

            Assert.True(provider.ValueKind != JsonValueKind.Undefined,
                $"Provider '{name}' not found in list");
            Assert.True(provider.GetProperty("configured").GetBoolean(),
                $"Provider '{name}' should be configured=true");
        }
    }
}
