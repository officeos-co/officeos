using System.Net;
using System.Text.Json;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Features.Providers;
using OffceOs.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Billing;

public sealed class LlmProviderDispatcherUsageTests
{
    [Fact]
    public async Task OpenAi_compatible_providers_request_stream_usage_for_all_supported_api_providers()
    {
        var providers = ProviderRegistry.All
            .Where(p => p.ApiFormat == ApiFormat.OpenAiCompat)
            .ToList();

        foreach (var provider in providers)
        {
            var handler = new CapturingHandler(_ => HttpResponseFactory.SseResponse("""
                data: {"choices":[],"usage":{"prompt_tokens":11,"completion_tokens":7}}

                data: [DONE]

                """));
            var dispatcher = new LlmProviderDispatcher(new FakeHttpClientFactory(handler), NullLogger<LlmProviderDispatcher>.Instance);

            var result = await dispatcher.DispatchAsync(
                provider.Slug,
                "test-key",
                LlmProviderDispatcherTestData.ModelFor(provider),
                LlmProviderDispatcherTestData.RequestBody(LlmProviderDispatcherTestData.ModelFor(provider)),
                CancellationToken.None);

            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
            Assert.Equal(LlmProviderDispatcherTestData.ModelFor(provider), result.Value.Model);

            var sent = Assert.Single(handler.Requests);
            Assert.Equal("Bearer", sent.AuthorizationScheme);
            Assert.Equal("test-key", sent.AuthorizationParameter);

            using var doc = JsonDocument.Parse(sent.Body);
            Assert.Equal(LlmProviderDispatcherTestData.ModelFor(provider), doc.RootElement.GetProperty("model").GetString());
            Assert.True(doc.RootElement.GetProperty("stream_options").GetProperty("include_usage").GetBoolean());
        }
    }

    [Fact]
    public async Task OpenAi_compatible_provider_retries_without_stream_options_when_provider_rejects_it()
    {
        var calls = 0;
        var handler = new CapturingHandler(_ =>
        {
            calls++;
            return calls == 1
                ? HttpResponseFactory.JsonResponse(HttpStatusCode.BadRequest, """{"error":"unsupported parameter stream_options"}""")
                : HttpResponseFactory.SseResponse("data: [DONE]\n\n");
        });
        var dispatcher = new LlmProviderDispatcher(new FakeHttpClientFactory(handler), NullLogger<LlmProviderDispatcher>.Instance);

        var result = await dispatcher.DispatchAsync("openrouter", "test-key", "openrouter/model", LlmProviderDispatcherTestData.RequestBody("openrouter/model"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("stream_options", handler.Requests[0].Body);
        Assert.DoesNotContain("stream_options", handler.Requests[1].Body);
    }

    [Fact]
    public async Task Custom_provider_uses_declared_base_url_without_authorization_when_key_is_empty()
    {
        var handler = new CapturingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var dispatcher = new LlmProviderDispatcher(new FakeHttpClientFactory(handler), NullLogger<LlmProviderDispatcher>.Instance);

        var result = await dispatcher.DispatchAsync(
            "custom",
            new ProviderAuthResult(ProviderAuthKind.ApiKey, new Dictionary<string, string> { ["baseUrl"] = "http://self-hosted:8000/v1" }),
            "deepseek-ai/DeepSeek-R1-Distill-Qwen-32B",
            LlmProviderDispatcherTestData.RequestBody("deepseek-ai/DeepSeek-R1-Distill-Qwen-32B"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal("deepseek-ai/DeepSeek-R1-Distill-Qwen-32B", result.Value.Model);

        var sent = Assert.Single(handler.Requests);
        Assert.Equal("http://self-hosted:8000/v1/chat/completions", sent.RequestUri);
        Assert.Null(sent.AuthorizationScheme);

        using var doc = JsonDocument.Parse(sent.Body);
        Assert.Equal("deepseek-ai/DeepSeek-R1-Distill-Qwen-32B", doc.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task Custom_provider_sends_bearer_authorization_when_key_is_configured()
    {
        var handler = new CapturingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var dispatcher = new LlmProviderDispatcher(new FakeHttpClientFactory(handler), NullLogger<LlmProviderDispatcher>.Instance);

        var result = await dispatcher.DispatchAsync(
            "custom",
            new ProviderAuthResult(
                ProviderAuthKind.ApiKey,
                new Dictionary<string, string>
                {
                    ["baseUrl"] = "http://self-hosted:8000/v1",
                    ["apiKey"] = "custom-key",
                }),
            "deepseek-r1:8b",
            LlmProviderDispatcherTestData.RequestBody("deepseek-r1:8b"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal("Bearer", sent.AuthorizationScheme);
        Assert.Equal("custom-key", sent.AuthorizationParameter);
    }

    [Fact]
    public async Task Anthropic_provider_translates_usage_into_openai_compatible_stream()
    {
        var handler = new CapturingHandler(_ => HttpResponseFactory.SseResponse("""
            data: {"type":"message_start","message":{"usage":{"input_tokens":19,"output_tokens":1}}}

            data: {"type":"content_block_delta","delta":{"type":"text_delta","text":"hello"}}

            data: {"type":"message_delta","usage":{"output_tokens":5}}

            data: {"type":"message_stop"}

            """));
        var dispatcher = new LlmProviderDispatcher(new FakeHttpClientFactory(handler), NullLogger<LlmProviderDispatcher>.Instance);

        var result = await dispatcher.DispatchAsync("anthropic", "test-key", "claude-haiku-4-5", LlmProviderDispatcherTestData.RequestBody("claude-haiku-4-5"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var streamText = await result.Value.Response.Content.ReadAsStringAsync();

        Assert.Contains("\"prompt_tokens\":19", streamText);
        Assert.Contains("\"completion_tokens\":5", streamText);
        Assert.Contains("data: [DONE]", streamText);

        var sent = Assert.Single(handler.Requests);
        Assert.Equal("x-api-key", sent.ApiKeyHeaderName);
        Assert.Equal("test-key", sent.ApiKeyHeaderValue);
    }

    [Fact]
    public void ProviderRegistry_calculates_expected_credit_weights_for_billable_models()
    {
        Assert.Equal(10, ProviderRegistry.ToCredits("gpt-4o-mini", 10));
        Assert.Equal(150, ProviderRegistry.ToCredits("gpt-4o", 10));
        Assert.Equal(50, ProviderRegistry.ToCredits("claude-haiku-4-5", 10));
        Assert.Equal(200, ProviderRegistry.ToCredits("claude-sonnet-4-6", 10));
        Assert.Equal(750, ProviderRegistry.ToCredits("claude-opus-4-6", 10));
        Assert.Equal(80, ProviderRegistry.ToCredits("gemini-2.5-pro", 10));
    }

}
