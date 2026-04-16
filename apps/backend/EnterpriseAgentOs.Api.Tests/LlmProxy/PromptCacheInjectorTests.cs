namespace EnterpriseAgentOs.Api.Tests.LlmProxy;

public sealed class PromptCacheInjectorTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();

    private static JsonElement GetMessages(JsonElement body) =>
        body.GetProperty("messages");

    private static JsonElement GetTools(JsonElement body) =>
        body.GetProperty("tools");

    // ── non-Claude model ──────────────────────────────────────────────────────

    [Fact]
    public void NonClaudeModel_BodyReturnedUnchanged()
    {
        var body = Parse("""{"messages":[{"role":"system","content":"You are helpful"}]}""");
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "gpt-4o");
        Assert.Equal(body.GetRawText(), result.GetRawText());
    }

    [Fact]
    public void GeminiModel_BodyReturnedUnchanged()
    {
        var body = Parse("""{"messages":[{"role":"system","content":"You are helpful"}]}""");
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "gemini-2.5-pro");
        Assert.Equal(body.GetRawText(), result.GetRawText());
    }

    // ── system message with string content ────────────────────────────────────

    [Fact]
    public void SystemMessage_StringContent_WrappedInArrayWithCacheControl()
    {
        var body = Parse("""
            {
                "messages": [
                    {"role": "system", "content": "You are an agent."}
                ]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-sonnet-4-6");
        var messages = GetMessages(result);

        Assert.Equal(1, messages.GetArrayLength());
        var sysMsg = messages[0];
        Assert.Equal("system", sysMsg.GetProperty("role").GetString());

        var content = sysMsg.GetProperty("content");
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        Assert.Equal(1, content.GetArrayLength());

        var elem = content[0];
        Assert.Equal("text", elem.GetProperty("type").GetString());
        Assert.Equal("You are an agent.", elem.GetProperty("text").GetString());
        Assert.Equal("ephemeral", elem.GetProperty("cache_control").GetProperty("type").GetString());
    }

    // ── system message with array content ─────────────────────────────────────

    [Fact]
    public void SystemMessage_ArrayContent_CacheControlAddedToLastElement()
    {
        var body = Parse("""
            {
                "messages": [
                    {
                        "role": "system",
                        "content": [
                            {"type": "text", "text": "First part"},
                            {"type": "text", "text": "Second part"}
                        ]
                    }
                ]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-haiku-4-5");
        var content = GetMessages(result)[0].GetProperty("content");

        Assert.Equal(2, content.GetArrayLength());

        // First element should NOT have cache_control
        Assert.False(content[0].TryGetProperty("cache_control", out _));

        // Last element should have cache_control
        var last = content[1];
        Assert.Equal("Second part", last.GetProperty("text").GetString());
        Assert.Equal("ephemeral", last.GetProperty("cache_control").GetProperty("type").GetString());
    }

    // ── multiple system messages ──────────────────────────────────────────────

    [Fact]
    public void MultipleSystemMessages_AllGetCacheControl()
    {
        var body = Parse("""
            {
                "messages": [
                    {"role": "system", "content": "SOUL.md content"},
                    {"role": "system", "content": "IDENTITY.md content"},
                    {"role": "user", "content": "Hello"}
                ]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-sonnet-4-6");
        var messages = GetMessages(result);

        // Both system messages get cache_control
        var sys1Content = messages[0].GetProperty("content");
        Assert.Equal(JsonValueKind.Array, sys1Content.ValueKind);
        Assert.Equal("ephemeral", sys1Content[0].GetProperty("cache_control").GetProperty("type").GetString());

        var sys2Content = messages[1].GetProperty("content");
        Assert.Equal(JsonValueKind.Array, sys2Content.ValueKind);
        Assert.Equal("ephemeral", sys2Content[0].GetProperty("cache_control").GetProperty("type").GetString());

        // User message should be untouched
        var userMsg = messages[2];
        Assert.Equal("user", userMsg.GetProperty("role").GetString());
        Assert.Equal("Hello", userMsg.GetProperty("content").GetString());
    }

    // ── tools array ───────────────────────────────────────────────────────────

    [Fact]
    public void ToolsArray_LastToolGetsCacheControl()
    {
        var body = Parse("""
            {
                "messages": [{"role": "user", "content": "Hi"}],
                "tools": [
                    {"name": "search", "description": "Search the web"},
                    {"name": "calculator", "description": "Do math"}
                ]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-sonnet-4-6");
        var tools = GetTools(result);

        Assert.Equal(2, tools.GetArrayLength());

        // First tool — no cache_control
        Assert.False(tools[0].TryGetProperty("cache_control", out _));

        // Last tool — has cache_control
        var last = tools[1];
        Assert.Equal("calculator", last.GetProperty("name").GetString());
        Assert.Equal("ephemeral", last.GetProperty("cache_control").GetProperty("type").GetString());
    }

    [Fact]
    public void SingleTool_GetsCacheControl()
    {
        var body = Parse("""
            {
                "messages": [{"role": "user", "content": "Hi"}],
                "tools": [{"name": "search", "description": "Search"}]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-haiku-4-5");
        var tools = GetTools(result);

        Assert.Equal(1, tools.GetArrayLength());
        Assert.Equal("ephemeral", tools[0].GetProperty("cache_control").GetProperty("type").GetString());
    }

    // ── no system message ─────────────────────────────────────────────────────

    [Fact]
    public void NoSystemMessage_BodyUnchanged_NoCrash()
    {
        var body = Parse("""
            {
                "messages": [
                    {"role": "user", "content": "Hello"},
                    {"role": "assistant", "content": "Hi there"}
                ]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-sonnet-4-6");
        var messages = GetMessages(result);

        Assert.Equal(2, messages.GetArrayLength());
        // Neither user nor assistant message should have content modified
        Assert.Equal("Hello", messages[0].GetProperty("content").GetString());
        Assert.Equal("Hi there", messages[1].GetProperty("content").GetString());
    }

    // ── both system + tools ───────────────────────────────────────────────────

    [Fact]
    public void SystemAndTools_BothGetCacheControl()
    {
        var body = Parse("""
            {
                "messages": [
                    {"role": "system", "content": "You are an agent."},
                    {"role": "user", "content": "Do something"}
                ],
                "tools": [
                    {"name": "tool_a", "description": "First tool"},
                    {"name": "tool_b", "description": "Last tool"}
                ]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-sonnet-4-6");

        // System message gets cache_control
        var sysContent = GetMessages(result)[0].GetProperty("content");
        Assert.Equal(JsonValueKind.Array, sysContent.ValueKind);
        Assert.Equal("ephemeral", sysContent[0].GetProperty("cache_control").GetProperty("type").GetString());

        // Last tool gets cache_control
        var tools = GetTools(result);
        Assert.False(tools[0].TryGetProperty("cache_control", out _));
        Assert.Equal("ephemeral", tools[1].GetProperty("cache_control").GetProperty("type").GetString());
    }

    // ── non-system messages not modified ─────────────────────────────────────

    [Fact]
    public void UserAndAssistantMessages_NotModified()
    {
        var body = Parse("""
            {
                "messages": [
                    {"role": "user", "content": "Hello"},
                    {"role": "assistant", "content": "World"},
                    {"role": "user", "content": "More"}
                ]
            }
            """);

        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "claude-sonnet-4-6");
        var messages = GetMessages(result);

        foreach (var msg in messages.EnumerateArray())
        {
            var content = msg.GetProperty("content");
            // None of these should be arrays with cache_control
            if (content.ValueKind == JsonValueKind.Array)
            {
                foreach (var elem in content.EnumerateArray())
                {
                    Assert.False(elem.TryGetProperty("cache_control", out _));
                }
            }
        }
    }

    // ── Claude model name variations ──────────────────────────────────────────

    [Fact]
    public void ClaudeModelCaseInsensitive_Uppercase_AppliesCache()
    {
        var body = Parse("""{"messages":[{"role":"system","content":"Test"}]}""");
        var result = EnterpriseAgentOs.Api.Entities.LlmProxy.PromptCacheInjector.Inject(body, "Claude-3-5-Sonnet");
        var content = GetMessages(result)[0].GetProperty("content");
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        Assert.True(content[0].TryGetProperty("cache_control", out _));
    }
}
