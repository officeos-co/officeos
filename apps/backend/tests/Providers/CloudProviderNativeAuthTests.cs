using System.Net;
using System.Text.Json;
using OffceOs.Configuration;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Features.Providers;
using OffceOs.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Providers;

public sealed class CloudProviderNativeAuthTests
{
    [Fact]
    public async Task Aws_bedrock_iam_auth_signs_runtime_request_with_sigv4_headers()
    {
        var handler = new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var dispatcher = new LlmProviderDispatcher(
            new FakeHttpClientFactory(handler),
            NullLogger<LlmProviderDispatcher>.Instance,
            utcNow: () => new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var result = await dispatcher.DispatchAsync(
            ProviderRegistry.AwsBedrockProviderSlug,
            new ProviderAuthResult(
                ProviderAuthKind.AwsIam,
                new Dictionary<string, string>
                {
                    ["awsAccessKeyId"] = "AKIATEST",
                    ["awsSecretAccessKey"] = "secret",
                    ["awsSessionToken"] = "session-token",
                    ["awsRegion"] = "us-east-1",
                }),
            "anthropic.claude-sonnet-4-20250514-v1:0",
            LlmProviderDispatcherTestData.RequestBody("anthropic.claude-sonnet-4-20250514-v1:0"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            "https://bedrock-runtime.us-east-1.amazonaws.com/model/anthropic.claude-sonnet-4-20250514-v1%3A0/invoke-with-response-stream",
            request.RequestUri!.ToString());
        var authorization = request.Headers.GetValues("Authorization").Single();
        Assert.StartsWith("AWS4-HMAC-SHA256", authorization);
        Assert.Contains("Credential=AKIATEST/20260510/us-east-1/bedrock/aws4_request", authorization);
        Assert.True(request.Headers.Contains("x-amz-date"));
        Assert.True(request.Headers.Contains("x-amz-content-sha256"));
        Assert.Equal("session-token", request.Headers.GetValues("x-amz-security-token").Single());
    }

    [Fact]
    public async Task Google_vertex_service_account_auth_uses_oauth_bearer_token_and_vertex_endpoint()
    {
        var handler = new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var dispatcher = new LlmProviderDispatcher(
            new FakeHttpClientFactory(handler),
            NullLogger<LlmProviderDispatcher>.Instance,
            cloudProviderTokenService: new FakeCloudProviderTokenService(googleToken: "google-token", azureToken: "unused"));

        var result = await dispatcher.DispatchAsync(
            ProviderRegistry.GoogleVertexProviderSlug,
            new ProviderAuthResult(
                ProviderAuthKind.GoogleServiceAccount,
                new Dictionary<string, string>
                {
                    ["serviceAccountJson"] = """{"client_email":"svc@example.com"}""",
                    ["projectId"] = "acme-project",
                    ["location"] = "us-east5",
                }),
            "claude-sonnet-4@20250514",
            LlmProviderDispatcherTestData.RequestBody("claude-sonnet-4@20250514"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            "https://us-east5-aiplatform.googleapis.com/v1/projects/acme-project/locations/us-east5/publishers/anthropic/models/claude-sonnet-4%4020250514:streamRawPredict",
            request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("google-token", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Azure_foundry_entra_auth_uses_ai_scope_bearer_token_and_resource_endpoint()
    {
        var handler = new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var dispatcher = new LlmProviderDispatcher(
            new FakeHttpClientFactory(handler),
            NullLogger<LlmProviderDispatcher>.Instance,
            cloudProviderTokenService: new FakeCloudProviderTokenService(googleToken: "unused", azureToken: "azure-token"));

        var result = await dispatcher.DispatchAsync(
            ProviderRegistry.AzureFoundryProviderSlug,
            new ProviderAuthResult(
                ProviderAuthKind.AzureEntraClientSecret,
                new Dictionary<string, string>
                {
                    ["tenantId"] = "tenant",
                    ["clientId"] = "client",
                    ["clientSecret"] = "secret",
                    ["endpoint"] = "https://acme.openai.azure.com",
                }),
            "claude-sonnet-4-20250514",
            LlmProviderDispatcherTestData.RequestBody("claude-sonnet-4-20250514"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://acme.openai.azure.com/openai/v1/chat/completions", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("azure-token", request.Headers.Authorization.Parameter);

        using var doc = JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal("claude-sonnet-4-20250514", doc.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task Azure_foundry_api_key_auth_sends_api_key_header_not_bearer()
    {
        var handler = new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"));
        var dispatcher = new LlmProviderDispatcher(
            new FakeHttpClientFactory(handler),
            NullLogger<LlmProviderDispatcher>.Instance);

        var result = await dispatcher.DispatchAsync(
            ProviderRegistry.AzureFoundryProviderSlug,
            new ProviderAuthResult(
                ProviderAuthKind.AzureApiKey,
                new Dictionary<string, string>
                {
                    ["apiKey"] = "foundry-key",
                    ["endpoint"] = "https://acme.openai.azure.com/",
                }),
            "claude-sonnet-4-20250514",
            LlmProviderDispatcherTestData.RequestBody("claude-sonnet-4-20250514"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var request = Assert.Single(handler.Requests);
        Assert.Null(request.Headers.Authorization);
        Assert.Equal("foundry-key", request.Headers.GetValues("api-key").Single());
    }

    private sealed class FakeCloudProviderTokenService : ICloudProviderTokenService
    {
        private readonly string _googleToken;
        private readonly string _azureToken;

        public FakeCloudProviderTokenService(string googleToken, string azureToken)
        {
            _googleToken = googleToken;
            _azureToken = azureToken;
        }

        public Task<string> GetGoogleAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default) =>
            Task.FromResult(_googleToken);

        public Task<string> GetAzureAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default) =>
            Task.FromResult(_azureToken);
    }
}
