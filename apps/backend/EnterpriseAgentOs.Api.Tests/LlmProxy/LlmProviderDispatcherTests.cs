namespace EnterpriseAgentOs.Api.Tests.LlmProxy;

public sealed class LlmProviderDispatcherTests
{
    [Theory]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("google")]
    [InlineData("xai")]
    [InlineData("groq")]
    [InlineData("deepseek")]
    [InlineData("openrouter")]
    public void IsSupported_AllExpectedProviders_ReturnsTrue(string provider)
    {
        var dispatcher = new EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProviderDispatcher(
            new FakeHttpClientFactory(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProviderDispatcher>.Instance);

        Assert.True(dispatcher.IsSupported(provider));
    }

    [Fact]
    public void IsSupported_Google_ReturnsTrue()
    {
        var dispatcher = new EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProviderDispatcher(
            new FakeHttpClientFactory(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProviderDispatcher>.Instance);

        Assert.True(dispatcher.IsSupported("google"));
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
