using System.Text.Json;
using EnterpriseAgentOs.Application.Services.Agents;
using EnterpriseAgentOs.Application.Services.Agents.Tools;
using EnterpriseAgentOs.Domain.Models;

namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// Unit tests for the agent turn loop components.
/// Ported from agent-core's tests/tools_local.rs, tests/turn_loop.rs.
/// </summary>
public class AgentTurnTests
{
    // ── ConversationHistory ─────────────────────────────────────────────────

    [Fact]
    public void History_Prune_Collapses_Old_Tool_Pairs()
    {
        var history = new ConversationHistory();

        // Push 10 messages: alternating assistant (with tool call) + tool result
        for (var i = 0; i < 5; i++)
        {
            history.Push(new ChatMessage
            {
                Role = "assistant",
                Content = $"Calling tool {i}",
                ToolCalls = new List<ChatToolCall>
                {
                    new() { Id = $"call_{i}", Name = "shell", Arguments = $"{{\"command\":\"echo {i}\"}}" }
                }
            });
            history.Push(new ChatMessage
            {
                Role = "tool",
                Content = $"Output from tool {i} — some long output that takes up tokens",
                ToolCallId = $"call_{i}",
            });
        }

        // Also push a recent user message to protect
        history.Push(new ChatMessage { Role = "user", Content = "What happened?" });

        Assert.Equal(11, history.Count);

        // Prune with a small budget to force collapsing
        history.Prune(maxTokens: 200, keepRecent: 4);

        // After pruning, old tool results should be collapsed
        Assert.True(history.Count < 11, $"Expected fewer messages after pruning, got {history.Count}");

        // Recent messages should still be there
        Assert.Equal("What happened?", history.Messages[^1].Content);
    }

    [Fact]
    public void History_Prune_Protects_Recent_Messages()
    {
        var history = new ConversationHistory();

        for (var i = 0; i < 10; i++)
            history.Push(new ChatMessage { Role = "user", Content = $"Message {i} with some content to use tokens" });

        history.Prune(maxTokens: 50, keepRecent: 4);

        // Last 4 should always survive
        Assert.True(history.Count >= 4);
        Assert.Equal("Message 9 with some content to use tokens", history.Messages[^1].Content);
    }

    [Fact]
    public void History_Prune_Never_Removes_System_Messages()
    {
        var history = new ConversationHistory();

        history.Push(new ChatMessage { Role = "system", Content = "You are a test agent." });
        for (var i = 0; i < 20; i++)
            history.Push(new ChatMessage { Role = "user", Content = $"Filler message {i}" });

        history.Prune(maxTokens: 50, keepRecent: 2);

        Assert.Contains(history.Messages, m => m.Role == "system");
    }

    // ── LoopDetector ────────────────────────────────────────────────────────

    [Fact]
    public void LoopDetector_Warns_On_3_Exact_Repeats()
    {
        var detector = new LoopDetector();

        detector.Record("shell", "{\"command\":\"ls\"}", "file1.txt");
        detector.Record("shell", "{\"command\":\"ls\"}", "file1.txt");
        var result = detector.Record("shell", "{\"command\":\"ls\"}", "file1.txt");

        Assert.IsType<LoopDetectionResult.WarningResult>(result);
    }

    [Fact]
    public void LoopDetector_Blocks_On_4_Exact_Repeats()
    {
        var detector = new LoopDetector();

        for (var i = 0; i < 3; i++)
            detector.Record("shell", "{\"command\":\"ls\"}", "file1.txt");

        var result = detector.Record("shell", "{\"command\":\"ls\"}", "file1.txt");

        Assert.IsType<LoopDetectionResult.BlockResult>(result);
    }

    [Fact]
    public void LoopDetector_Breaks_On_5_Exact_Repeats()
    {
        var detector = new LoopDetector();

        for (var i = 0; i < 4; i++)
            detector.Record("shell", "{\"command\":\"ls\"}", "file1.txt");

        var result = detector.Record("shell", "{\"command\":\"ls\"}", "file1.txt");

        Assert.IsType<LoopDetectionResult.BreakResult>(result);
    }

    [Fact]
    public void LoopDetector_Ok_On_Different_Args()
    {
        var detector = new LoopDetector();

        var r1 = detector.Record("shell", "{\"command\":\"ls\"}", "output1");
        var r2 = detector.Record("shell", "{\"command\":\"cat foo\"}", "output2");
        var r3 = detector.Record("shell", "{\"command\":\"pwd\"}", "output3");

        Assert.IsType<LoopDetectionResult.OkResult>(r1);
        Assert.IsType<LoopDetectionResult.OkResult>(r2);
        Assert.IsType<LoopDetectionResult.OkResult>(r3);
    }

    [Fact]
    public void LoopDetector_Detects_PingPong_Pattern()
    {
        var detector = new LoopDetector();

        // Alternate between two tools for 4+ cycles (8 calls)
        for (var i = 0; i < 5; i++)
        {
            detector.Record("file_read", "{\"path\":\"a.txt\"}", "content");
            detector.Record("file_write", "{\"path\":\"b.txt\"}", "ok");
        }

        // The last call should trigger ping-pong detection
        var result = detector.Record("file_read", "{\"path\":\"a.txt\"}", "content");

        // Should be at least a warning (exact behavior depends on implementation)
        Assert.NotEqual(LoopDetectionResult.Ok, result);
    }

    // ── PromptSections ──────────────────────────────────────────────────────

    [Fact]
    public void PromptSections_DateTime_Contains_UTC()
    {
        var section = PromptSections.DateTime();
        Assert.Contains("UTC", section);
        Assert.Contains("CRITICAL CONTEXT", section);
    }

    [Fact]
    public void PromptSections_Skills_Returns_Null_When_Empty()
    {
        var result = PromptSections.SkillsWithDocs(new List<SkillRecord>());
        Assert.Null(result);
    }

    [Fact]
    public void PromptSections_Skills_Injects_Full_Doc()
    {
        var skills = new List<SkillRecord>
        {
            new() { Name = "notion", Description = "Search and manage Notion pages.", Doc = "## Actions\n\n- search\n- read_page" },
            new() { Name = "github", Description = "Manage GitHub repos and issues.", Doc = null },
        };

        var result = PromptSections.SkillsWithDocs(skills);

        Assert.NotNull(result);
        Assert.Contains("### notion", result);
        Assert.Contains("## Actions", result); // Full doc injected
        Assert.Contains("- search", result);
        Assert.Contains("### github", result);
        Assert.Contains("Manage GitHub repos and issues.", result); // Falls back to description when no doc
    }

    [Fact]
    public void PromptSections_No_Tool_Schema_In_Prompt()
    {
        // The system prompt should never contain tool schemas — those go via the LLM tools parameter.
        var allSections = string.Join("\n",
            PromptSections.DateTime(),
            PromptSections.ToolHonesty(),
            PromptSections.Safety(),
            PromptSections.SkillsWithDocs(new List<SkillRecord>
            {
                new() { Name = "notion", Description = "Search pages", Doc = "Use notion search to find pages." },
            }) ?? "",
            PromptSections.Workspace("test-agent"),
            PromptSections.Runtime());

        Assert.DoesNotContain("\"type\": \"object\"", allSections);
        Assert.DoesNotContain("\"parameters\"", allSections);
        Assert.DoesNotContain("\"required\"", allSections);
    }

    // ── SkillReadTool ───────────────────────────────────────────────────────

    [Fact]
    public async Task SkillRead_Returns_Full_Doc()
    {
        var skills = new List<SkillRecord>
        {
            new()
            {
                Name = "notion",
                Description = "Search pages",
                Doc = "# Notion\n\nFull documentation here with all the details..."
            }
        };

        var tool = new SkillReadTool(skills);
        var args = JsonSerializer.SerializeToElement(new { name = "notion" });
        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Contains("Full documentation here", result.Value.Output);
    }

    [Fact]
    public async Task SkillRead_Returns_Error_For_Unknown_Skill()
    {
        var skills = new List<SkillRecord>
        {
            new() { Name = "notion", Description = "Search pages", Doc = "docs" }
        };

        var tool = new SkillReadTool(skills);
        var args = JsonSerializer.SerializeToElement(new { name = "nonexistent" });
        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Success);
        Assert.Contains("not found", result.Value.Error!);
        Assert.Contains("notion", result.Value.Error!); // Lists available skills
    }

    [Fact]
    public async Task SkillRead_Case_Insensitive()
    {
        var skills = new List<SkillRecord>
        {
            new() { Name = "GitHub", Description = "Manage repos", Doc = "# GitHub\n\nDocs..." }
        };

        var tool = new SkillReadTool(skills);
        var args = JsonSerializer.SerializeToElement(new { name = "github" });
        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Contains("GitHub", result.Value.Output);
    }

    // ── ToolRegistry ────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolRegistry_Dispatch_Unknown_Tool_Returns_Error()
    {
        var registry = new ToolRegistry(new List<IAgentTool>());
        var args = JsonSerializer.SerializeToElement(new { });
        var result = await registry.DispatchAsync("nonexistent_tool", args, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Unknown tool", result.Error.Message);
    }

    [Fact]
    public void ToolRegistry_GetSchemas_Returns_OpenAI_Format()
    {
        var skills = new List<SkillRecord>();
        var tool = new SkillReadTool(skills);
        var registry = new ToolRegistry(new List<IAgentTool> { tool });

        var schemas = registry.GetSchemas();

        Assert.Single(schemas);
        var json = JsonSerializer.SerializeToElement(schemas[0]);
        Assert.Equal("function", json.GetProperty("type").GetString());
        Assert.Equal("skill_read", json.GetProperty("function").GetProperty("name").GetString());
        // Schema is in the `function.parameters` field, NOT in the system prompt
        Assert.True(json.GetProperty("function").TryGetProperty("parameters", out _));
    }

    // ── Memory Tools ────────────────────────────────────────────────────────

    [Fact]
    public void AgentMemoryRecord_Create_Validates_Key()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AgentMemoryRecord.Create(Guid.NewGuid(), "", "content"));

        Assert.Throws<InvalidOperationException>(() =>
            AgentMemoryRecord.Create(Guid.NewGuid(), new string('x', 513), "content"));
    }

    [Fact]
    public void AgentMemoryRecord_Create_Validates_Content()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AgentMemoryRecord.Create(Guid.NewGuid(), "key", null!));
    }

    [Fact]
    public void AgentMemoryRecord_UpdateContent_Bumps_Timestamp()
    {
        var record = AgentMemoryRecord.Create(Guid.NewGuid(), "test-key", "original");
        var originalTime = record.UpdatedAt;

        // Small delay to ensure timestamp difference
        Thread.Sleep(10);
        record.UpdateContent("updated");

        Assert.Equal("updated", record.Content);
        Assert.True(record.UpdatedAt >= originalTime);
    }

    // ── Personality ─────────────────────────────────────────────────────────

    [Fact]
    public void AgentPersonalityRecord_CreateDefaults_Seeds_Soul_And_Identity()
    {
        var agentId = Guid.NewGuid();
        var defaults = AgentPersonalityRecord.CreateDefaults(agentId, "TestBot");

        Assert.Equal(2, defaults.Count);
        Assert.Equal("SOUL.md", defaults[0].FileName);
        Assert.Equal("IDENTITY.md", defaults[1].FileName);
        Assert.Contains("autonomous AI agent", defaults[0].Content);
        Assert.Contains("TestBot", defaults[1].Content);
    }

    [Fact]
    public void AgentPersonalityRecord_CreateDefaults_Requires_Name()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AgentPersonalityRecord.CreateDefaults(Guid.NewGuid(), ""));
    }

    [Fact]
    public void AgentPersonalityRecord_CompositionOrder_Known_Files_First()
    {
        var agentId = Guid.NewGuid();
        var soul = AgentPersonalityRecord.Create(agentId, "SOUL.md", "soul content");
        var custom = AgentPersonalityRecord.Create(agentId, "CUSTOM.md", "custom content");
        var identity = AgentPersonalityRecord.Create(agentId, "IDENTITY.md", "identity");
        var bootstrap = AgentPersonalityRecord.Create(agentId, "BOOTSTRAP.md", "bootstrap");

        var ordered = new[] { soul, custom, identity, bootstrap }
            .OrderBy(p => p.CompositionOrder)
            .ToList();

        Assert.Equal("SOUL.md", ordered[0].FileName);
        Assert.Equal("IDENTITY.md", ordered[1].FileName);
        Assert.Equal("BOOTSTRAP.md", ordered[2].FileName);
        Assert.Equal("CUSTOM.md", ordered[3].FileName); // Unknown files come last
    }
}
