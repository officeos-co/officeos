namespace EnterpriseAgentOs.Api.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GDPR compliance tests
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GdprTests : IClassFixture<EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory>
{
    private readonly EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory _factory;

    public GdprTests(EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    // ── 1. Export returns user data ──────────────────────────────────────────

    [Fact]
    public async Task GdprExport_ReturnsUserDataAsJson()
    {
        var email = $"gdpr-export-{Guid.NewGuid():N}@example.com";
        var dashClient = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory, email: email);
        await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAgentAsync(dashClient, "export-test-agent");

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
        var dashClient = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory, email: email);
        var agentId = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAgentAsync(dashClient, "purge-test-agent");

        // Confirm the agent exists before purge
        var beforeData = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLAsync(
            dashClient,
            "query($id: UUID!) { agent(id: $id) { id } }",
            new { id = agentId });
        Assert.Equal(JsonValueKind.Object, beforeData.GetProperty("agent").ValueKind);

        // Issue GDPR purge (still REST)
        var purgeResponse = await dashClient.DeleteAsync("/api/gdpr/purge");
        Assert.Equal(HttpStatusCode.NoContent, purgeResponse.StatusCode);

        // The same session cookie is now invalid — any authenticated GraphQL call errors
        var afterAuth = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLRawAsync(dashClient, "{ agents { id } }");
        Assert.True(afterAuth.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);

        // Verify the agent record is gone in the DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnterpriseAgentOs.Api.Database.EaosDbContext>();
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
// LLM prompt-injection guardrail tests (unit tests on InjectGuardrail)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GuardrailTests
{
    [Fact]
    public void InjectGuardrail_InsertsSystemMessageAtPosition0()
    {
        var body = JsonDocument.Parse("""
            {"messages":[{"role":"user","content":"hello"}]}
            """).RootElement.Clone();

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProxyController.InjectGuardrail(body);
        var messages = result.GetProperty("messages");

        Assert.True(messages.GetArrayLength() >= 2);

        var first = messages[0];
        Assert.Equal("system", first.GetProperty("role").GetString());
        Assert.Contains("prompt injection", first.GetProperty("content").GetString()!, StringComparison.OrdinalIgnoreCase);

        // Original user message preserved at position 1
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("hello", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public void InjectGuardrail_PrecedesExistingSystemPrompt()
    {
        const string originalSystem = "You are a helpful assistant for enterprise tasks.";
        var body = JsonDocument.Parse($$"""
            {"messages":[{"role":"system","content":"{{originalSystem}}"},{"role":"user","content":"Do something."}]}
            """).RootElement.Clone();

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProxyController.InjectGuardrail(body);
        var messages = result.GetProperty("messages");

        Assert.True(messages.GetArrayLength() >= 3);

        // Position 0: guardrail
        Assert.Contains("prompt injection", messages[0].GetProperty("content").GetString()!, StringComparison.OrdinalIgnoreCase);

        // Position 1: original system prompt
        Assert.Equal("system", messages[1].GetProperty("role").GetString());
        Assert.Equal(originalSystem, messages[1].GetProperty("content").GetString());

        // Position 2: user message
        Assert.Equal("user", messages[2].GetProperty("role").GetString());
    }

    [Fact]
    public void InjectGuardrail_NoMessages_ReturnsBodyUnchanged()
    {
        var body = JsonDocument.Parse("""{"stream":true}""").RootElement.Clone();
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProxyController.InjectGuardrail(body);

        Assert.True(result.TryGetProperty("stream", out var stream));
        Assert.True(stream.GetBoolean());
        Assert.False(result.TryGetProperty("messages", out _));
    }
}
