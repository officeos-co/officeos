namespace EnterpriseAgentOs.Api.Tests.LlmProxy;

public sealed class SmartRouterTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static JsonElement BuildBody(string messageContent, int toolCount = 0)
    {
        var messages = $"[{{\"role\":\"user\",\"content\":{JsonSerializer.Serialize(messageContent)}}}]";
        string toolsPart = toolCount > 0
            ? $",\"tools\":[{string.Join(",", Enumerable.Range(0, toolCount).Select(i => $"{{\"name\":\"tool{i}\"}}"))  }]"
            : "";
        return JsonDocument.Parse($"{{\"messages\":{messages}{toolsPart}}}").RootElement.Clone();
    }

    // ── explicit model pass-through ───────────────────────────────────────────

    [Fact]
    public void ExplicitModel_ReturnsUnchanged()
    {
        var body = BuildBody("hello");
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("gpt-4o", body);
        Assert.Equal("gpt-4o", result);
    }

    [Fact]
    public void ExplicitModel_ClaudeOpus_ReturnsUnchanged()
    {
        var body = BuildBody("hello");
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("claude-opus-4-6", body);
        Assert.Equal("claude-opus-4-6", result);
    }

    // ── anthropic family (default) ────────────────────────────────────────────

    [Fact]
    public void Auto_ShortMessage_Anthropic_ReturnsHaiku()
    {
        // 50 chars is well under SimpleMaxChars (800)
        var body = BuildBody(new string('x', 50));
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body);
        Assert.Equal("claude-haiku-4-5", result);
    }

    [Fact]
    public void Auto_LongMessage_Anthropic_ReturnsSonnet()
    {
        // 5 000 chars is above StandardMaxChars (4 000)
        var body = BuildBody(new string('x', 5_000));
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body);
        Assert.Equal("claude-sonnet-4-6", result);
    }

    [Fact]
    public void Auto_MediumMessage_Anthropic_ReturnsSonnet()
    {
        // 2 000 chars: simple < 800 is false, ≤ 4 000 is true → Standard → sonnet
        var body = BuildBody(new string('x', 2_000));
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body);
        Assert.Equal("claude-sonnet-4-6", result);
    }

    [Fact]
    public void Auto_ShortMessageManyTools_Anthropic_ReturnsSonnet()
    {
        // Short content but > 3 tools → not Simple tier
        var body = BuildBody(new string('x', 100), toolCount: 5);
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body);
        Assert.Equal("claude-sonnet-4-6", result);
    }

    // ── openai family ─────────────────────────────────────────────────────────

    [Fact]
    public void Auto_ShortMessage_OpenAI_ReturnsGptMini()
    {
        var body = BuildBody(new string('x', 50));
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body, preferredFamily: "openai");
        Assert.Equal("gpt-4o-mini", result);
    }

    [Fact]
    public void Auto_LongMessage_OpenAI_ReturnsGpt4o()
    {
        var body = BuildBody(new string('x', 5_000));
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body, preferredFamily: "openai");
        Assert.Equal("gpt-4o", result);
    }

    // ── google family ─────────────────────────────────────────────────────────

    [Fact]
    public void Auto_ShortMessage_Google_ReturnsFlash()
    {
        var body = BuildBody(new string('x', 50));
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body, preferredFamily: "google");
        Assert.Equal("gemini-2.5-flash", result);
    }

    [Fact]
    public void Auto_LongMessage_Google_ReturnsPro()
    {
        var body = BuildBody(new string('x', 5_000));
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body, preferredFamily: "google");
        Assert.Equal("gemini-2.5-pro", result);
    }

    // ── edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Auto_EmptyMessages_Anthropic_ReturnsHaiku()
    {
        var body = JsonDocument.Parse("{\"messages\":[]}").RootElement.Clone();
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body);
        Assert.Equal("claude-haiku-4-5", result);
    }

    [Fact]
    public void Auto_NoMessagesProperty_Anthropic_ReturnsHaiku()
    {
        var body = JsonDocument.Parse("{\"stream\":true}").RootElement.Clone();
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.SmartRouter.Resolve("auto", body);
        Assert.Equal("claude-haiku-4-5", result);
    }
}
