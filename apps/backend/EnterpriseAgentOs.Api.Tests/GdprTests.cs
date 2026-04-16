using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseAgentOs.Api.Database;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace EnterpriseAgentOs.Api.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GDPR compliance tests
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GdprTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GdprTests(CustomWebApplicationFactory factory) => _factory = factory;

    // ── 1. Export returns user data ──────────────────────────────────────────

    [Fact]
    public async Task GdprExport_ReturnsUserDataAsJson()
    {
        var email = $"gdpr-export-{Guid.NewGuid():N}@example.com";
        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory, email: email);
        await TestHelpers.CreateAgentAsync(dashClient, "export-test-agent");

        var response = await dashClient.GetAsync("/api/gdpr/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Must send a Content-Disposition attachment header
        var cd = response.Content.Headers.ContentDisposition;
        Assert.NotNull(cd);
        Assert.Equal("attachment", cd!.DispositionType, StringComparer.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(email, body, StringComparison.Ordinal);
    }

    // ── 2. Export requires auth ──────────────────────────────────────────────

    [Fact]
    public async Task GdprExport_RequiresAuth()
    {
        var anon = _factory.CreateClient();
        var response = await anon.GetAsync("/api/gdpr/export");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── 3. Purge deletes all user data ───────────────────────────────────────

    [Fact]
    public async Task GdprPurge_DeletesAllUserData()
    {
        var email = $"gdpr-purge-{Guid.NewGuid():N}@example.com";
        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory, email: email);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, "purge-test-agent");

        // Confirm the agent exists before purge
        var beforeData = await TestHelpers.GraphQLAsync(
            dashClient,
            "query($id: UUID!) { agent(id: $id) { id } }",
            new { id = agentId });
        Assert.Equal(JsonValueKind.Object, beforeData.GetProperty("agent").ValueKind);

        // Issue GDPR purge (still REST)
        var purgeResponse = await dashClient.DeleteAsync("/api/gdpr/purge");
        Assert.Equal(HttpStatusCode.NoContent, purgeResponse.StatusCode);

        // The same session cookie is now invalid — any authenticated GraphQL call errors
        var afterAuth = await TestHelpers.GraphQLRawAsync(dashClient, "{ agents { id } }");
        Assert.True(afterAuth.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);

        // Verify the agent record is gone in the DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();
        var agentInDb = await db.Agents.FindAsync(agentId);
        Assert.Null(agentInDb);
    }

    // ── 4. Purge requires auth ───────────────────────────────────────────────

    [Fact]
    public async Task GdprPurge_RequiresAuth()
    {
        var anon = _factory.CreateClient();
        var response = await anon.DeleteAsync("/api/gdpr/purge");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// LLM prompt-injection guardrail tests
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GuardrailTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    // Minimal valid JSON response that satisfies the proxy's streaming reader
    private const string FakeLlmResponse =
        """{"id":"test","object":"chat.completion","choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}],"usage":{"total_tokens":10}}""";

    public GuardrailTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;

        // Stub LiteLlm WireMock to accept any POST to /v1/chat/completions
        factory.LiteLlmMock
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(FakeLlmResponse));
    }

    // ── helper: parse the captured body forwarded to LiteLlm ─────────────────

    private JsonElement GetLastForwardedBody()
    {
        var entry = _factory.LiteLlmMock.LogEntries
            .LastOrDefault(e => e.RequestMessage.Path == "/v1/chat/completions"
                             && e.RequestMessage.Method == "POST");
        Assert.NotNull(entry);
        var bodyText = entry!.RequestMessage.Body;
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
        return JsonDocument.Parse(bodyText!).RootElement.Clone();
    }

    // ── 5. Guardrail injected before user messages ────────────────────────────

    [Fact]
    public async Task LlmProxy_InjectsGuardrailSystemMessage()
    {
        _factory.LiteLlmMock.ResetLogEntries();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, "guardrail-agent-1");

        var agentClient = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agentClient.ChatCompletionAsync(new
        {
            model = "claude-sonnet-4-6",
            messages = new[]
            {
                new { role = "user", content = "hello" }
            }
        });

        // Proxy must succeed or return a proxy-level error (not a 401/404)
        var status = (int)response.StatusCode;
        Assert.True(status < 500 || status == 502 || status == 200,
            $"Unexpected proxy failure: {status}");

        var forwarded = GetLastForwardedBody();
        var messages = forwarded.GetProperty("messages");

        // First message must be the guardrail system message
        Assert.True(messages.GetArrayLength() >= 2,
            "Expected at least the guardrail system message plus the original user message.");

        var firstMsg = messages[0];
        Assert.Equal("system", firstMsg.GetProperty("role").GetString());

        // The guardrail content must mention prompt injection
        var content = firstMsg.GetProperty("content");
        var contentText = content.ValueKind == JsonValueKind.String
            ? content.GetString()!
            : content.ToString();
        Assert.Contains("prompt injection", contentText, StringComparison.OrdinalIgnoreCase);
    }

    // ── 6. Guardrail precedes existing system prompt ──────────────────────────

    [Fact]
    public async Task LlmProxy_GuardrailPrecedesExistingSystemPrompt()
    {
        _factory.LiteLlmMock.ResetLogEntries();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, "guardrail-agent-2");

        const string originalSystemContent = "You are a helpful assistant for enterprise tasks.";

        var agentClient = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agentClient.ChatCompletionAsync(new
        {
            model = "claude-sonnet-4-6",
            messages = new object[]
            {
                new { role = "system", content = originalSystemContent },
                new { role = "user",   content = "Do something useful." }
            }
        });

        var status = (int)response.StatusCode;
        Assert.True(status < 500 || status == 502 || status == 200,
            $"Unexpected proxy failure: {status}");

        var forwarded = GetLastForwardedBody();
        var messages = forwarded.GetProperty("messages");

        // Must have at least 3 messages: guardrail + original system + user
        Assert.True(messages.GetArrayLength() >= 3,
            "Expected guardrail system message + original system message + user message.");

        // Position 0: guardrail
        var guardrail = messages[0];
        Assert.Equal("system", guardrail.GetProperty("role").GetString());
        var guardrailContent = guardrail.GetProperty("content");
        var guardrailText = guardrailContent.ValueKind == JsonValueKind.String
            ? guardrailContent.GetString()!
            : guardrailContent.ToString();
        Assert.Contains("prompt injection", guardrailText, StringComparison.OrdinalIgnoreCase);

        // Position 1: original system prompt (may be wrapped by PromptCacheInjector for claude models)
        var originalSys = messages[1];
        Assert.Equal("system", originalSys.GetProperty("role").GetString());
        var originalContent = originalSys.GetProperty("content");
        var originalText = originalContent.ValueKind == JsonValueKind.String
            ? originalContent.GetString()!
            : originalContent.ToString();
        Assert.Contains(originalSystemContent, originalText, StringComparison.Ordinal);
    }
}
