using OffceOs.Features.Providers.Application;
using OffceOs.Features.Providers.Domain;
using OffceOs.Features.Providers.Infrastructure;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.Providers;

public sealed class ProviderDispatchServiceTests
{
    [Fact]
    public async Task Dispatch_routes_regular_openai_to_llm_dispatcher()
    {
        var handler = new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var service = new ProviderDispatchService(
            new StaticProviderService(new ProviderAuthResult(
                ProviderAuthKind.ApiKey,
                new Dictionary<string, string> { ["apiKey"] = "sk-test" })),
            new LlmProviderDispatcher(
                new FakeHttpClientFactory(handler)),
            new FakeResourceLogService());
        using var document = JsonDocument.Parse("""{"messages":[],"stream":true}""");

        var result = await service.DispatchAsync("openai", null, "gpt-4o-mini", document.RootElement);

        Assert.True(result.IsSuccess);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Dispatch_routes_custom_provider_to_declared_base_url()
    {
        var handler = new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var service = new ProviderDispatchService(
            new StaticProviderService(new ProviderAuthResult(
                ProviderAuthKind.ApiKey,
                new Dictionary<string, string>
                {
                    ["baseUrl"] = "http://localhost:11434/v1",
                    ["apiKey"] = "local-key",
                })),
            new LlmProviderDispatcher(
                new FakeHttpClientFactory(handler)),
            new FakeResourceLogService());
        using var document = JsonDocument.Parse("""{"messages":[],"stream":true}""");

        var result = await service.DispatchAsync("custom", null, "llama3.1", document.RootElement);

        Assert.True(result.IsSuccess);
        Assert.Equal("llama3.1", result.Value.Model);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("http://localhost:11434/v1/chat/completions", request.RequestUri?.ToString());
        Assert.Equal("local-key", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Dispatch_routes_codex_provider_with_chatgpt_oauth_headers()
    {
        var handler = new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var service = new ProviderDispatchService(
            new StaticProviderService(new ProviderAuthResult(
                ProviderAuthKind.CodexChatGptOAuth,
                new Dictionary<string, string>
                {
                    ["accessToken"] = "codex-token",
                    ["accountId"] = "account-1",
                })),
            new LlmProviderDispatcher(
                new FakeHttpClientFactory(handler)),
            new FakeResourceLogService());
        using var document = JsonDocument.Parse("""{"messages":[],"stream":true}""");

        var result = await service.DispatchAsync("codex", null, "gpt-5.3-codex", document.RootElement);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://chatgpt.com/backend-api/codex/chat/completions", request.RequestUri?.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("codex-token", request.Headers.Authorization?.Parameter);
        Assert.Equal("account-1", request.Headers.GetValues("ChatGPT-Account-Id").Single());
    }

    private sealed class StaticProviderService : IProviderService
    {
        private readonly ProviderAuthResult _providerAuthResult;

        public StaticProviderService(ProviderAuthResult providerAuthResult) => _providerAuthResult = providerAuthResult;

        public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProviderResult>>([]);

        public Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProviderResult>>([]);

        public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default) =>
            Task.FromResult<ProviderAuthResult?>(_providerAuthResult);

        public Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<ProviderResourceAuthResult> AuthenticateCodexAsync(Guid workspaceId, CodexProviderAuthRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

}
