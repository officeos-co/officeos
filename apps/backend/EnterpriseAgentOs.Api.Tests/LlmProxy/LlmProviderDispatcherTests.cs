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
        var dispatcher = new LlmProviderDispatcher(
            new FakeHttpClientFactory(),
            NullLogger<LlmProviderDispatcher>.Instance);

        Assert.True(dispatcher.IsSupported(provider));
    }

    [Fact]
    public void IsSupported_Google_ReturnsTrue()
    {
        var dispatcher = new LlmProviderDispatcher(
            new FakeHttpClientFactory(),
            NullLogger<LlmProviderDispatcher>.Instance);

        Assert.True(dispatcher.IsSupported("google"));
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
