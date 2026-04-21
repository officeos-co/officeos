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
        Assert.Contains("Current Date", section);
    }

    [Fact]
    public void PromptSections_Skills_Returns_Null_When_Empty()
    {
        var result = PromptSections.Skills(new List<SkillRecord>());
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

        var result = PromptSections.Skills(skills);

        Assert.NotNull(result);
        Assert.Contains("### notion", result);
        Assert.Contains("## Actions", result);
        Assert.Contains("- search", result);
        Assert.Contains("### github", result);
        Assert.Contains("Manage GitHub repos and issues.", result);
    }

    [Fact]
    public void PromptSections_ProjectContext_Wraps_Files_In_Tags()
    {
        var files = new List<AgentPersonalityRecord>
        {
            AgentPersonalityRecord.Create(Guid.NewGuid(), "SOUL.md", "Be helpful."),
            AgentPersonalityRecord.Create(Guid.NewGuid(), "IDENTITY.md", "Your name is TestBot."),
        };

        var result = PromptSections.ProjectContext(files, null);

        Assert.NotNull(result);
        Assert.Contains("<file path=\"SOUL.md\">", result);
        Assert.Contains("Be helpful.", result);
        Assert.Contains("<file path=\"IDENTITY.md\">", result);
        Assert.Contains("</file>", result);
    }

    [Fact]
    public void PromptSections_ProjectContext_Includes_UserPrompt()
    {
        var result = PromptSections.ProjectContext(new List<AgentPersonalityRecord>(), "Custom instructions here.");

        Assert.NotNull(result);
        Assert.Contains("<file path=\"PROMPT.md\">", result);
        Assert.Contains("Custom instructions here.", result);
    }

    [Fact]
    public void PromptSections_Memory_Returns_Null_When_Empty()
    {
        var result = PromptSections.Memory(new List<AgentMemoryRecord>());
        Assert.Null(result);
    }

    [Fact]
    public void PromptSections_OpenClaw_Section_Order()
    {
        // Verify all sections compose in OpenClaw order without tool schemas.
        var allSections = string.Join("\n",
            PromptSections.Tooling(),
            PromptSections.Safety(),
            PromptSections.Skills(new List<SkillRecord>
            {
                new() { Name = "notion", Description = "Search pages", Doc = "Use notion search to find pages." },
            }) ?? "",
            PromptSections.Workspace("test-agent"),
            PromptSections.DateTime(),
            PromptSections.Runtime());

        Assert.DoesNotContain("\"type\": \"object\"", allSections);
        Assert.DoesNotContain("\"parameters\"", allSections);
        Assert.DoesNotContain("\"required\"", allSections);
        // Verify OpenClaw section order: Tooling before Safety before Skills
        var toolingIdx = allSections.IndexOf("## Tooling");
        var safetyIdx = allSections.IndexOf("## Safety");
        var skillsIdx = allSections.IndexOf("## Installed Skills");
        Assert.True(toolingIdx < safetyIdx);
        Assert.True(safetyIdx < skillsIdx);
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
    public void AgentPersonalityRecord_CreateDefaults_Seeds_OpenClaw_Files()
    {
        var agentId = Guid.NewGuid();
        var defaults = AgentPersonalityRecord.CreateDefaults(agentId, "TestBot");

        Assert.Equal(4, defaults.Count);
        var fileNames = defaults.Select(d => d.FileName).ToList();
        Assert.Contains("AGENTS.md", fileNames);
        Assert.Contains("SOUL.md", fileNames);
        Assert.Contains("IDENTITY.md", fileNames);
        Assert.Contains("USER.md", fileNames);

        var soul = defaults.First(d => d.FileName == "SOUL.md");
        Assert.Contains("Core Truths", soul.Content);
        Assert.Contains("genuinely helpful", soul.Content);

        var identity = defaults.First(d => d.FileName == "IDENTITY.md");
        Assert.Contains("TestBot", identity.Content);

        var agents = defaults.First(d => d.FileName == "AGENTS.md");
        Assert.Contains("Security & Boundaries", agents.Content);
    }

    [Fact]
    public void AgentPersonalityRecord_CreateDefaults_Requires_Name()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AgentPersonalityRecord.CreateDefaults(Guid.NewGuid(), ""));
    }

    [Fact]
    public void AgentPersonalityRecord_CompositionOrder_OpenClaw_Order()
    {
        var agentId = Guid.NewGuid();
        var agents = AgentPersonalityRecord.Create(agentId, "AGENTS.md", "rules");
        var soul = AgentPersonalityRecord.Create(agentId, "SOUL.md", "soul content");
        var tools = AgentPersonalityRecord.Create(agentId, "TOOLS.md", "tools");
        var identity = AgentPersonalityRecord.Create(agentId, "IDENTITY.md", "identity");
        var user = AgentPersonalityRecord.Create(agentId, "USER.md", "user");
        var memory = AgentPersonalityRecord.Create(agentId, "MEMORY.md", "memory");
        var bootstrap = AgentPersonalityRecord.Create(agentId, "BOOTSTRAP.md", "bootstrap");
        var custom = AgentPersonalityRecord.Create(agentId, "CUSTOM.md", "custom content");

        var ordered = new[] { custom, bootstrap, user, identity, tools, soul, agents, memory }
            .OrderBy(p => p.CompositionOrder)
            .ToList();

        // OpenClaw order: AGENTS → SOUL → TOOLS → IDENTITY → USER → MEMORY → BOOTSTRAP → custom
        Assert.Equal("AGENTS.md", ordered[0].FileName);
        Assert.Equal("SOUL.md", ordered[1].FileName);
        Assert.Equal("TOOLS.md", ordered[2].FileName);
        Assert.Equal("IDENTITY.md", ordered[3].FileName);
        Assert.Equal("USER.md", ordered[4].FileName);
        Assert.Equal("MEMORY.md", ordered[5].FileName);
        Assert.Equal("BOOTSTRAP.md", ordered[6].FileName);
        Assert.Equal("CUSTOM.md", ordered[7].FileName); // Unknown files come last
    }

    // ── SkillExecTool CLI parsing ─────────────────────────────────────────

    [Fact]
    public void CliParser_Strips_Quotes_From_Values()
    {
        var parsed = SkillExecParser.Parse(
            "google-calendar list_events --time_min \"2026-04-21T00:00:00Z\" --time_max \"2026-04-21T23:59:59Z\"");

        Assert.NotNull(parsed);
        Assert.Equal("google-calendar", parsed.Value.Skill);
        Assert.Equal("list_events", parsed.Value.Action);
        Assert.Equal("2026-04-21T00:00:00Z", parsed.Value.Args["time_min"]);
        Assert.Equal("2026-04-21T23:59:59Z", parsed.Value.Args["time_max"]);
    }

    [Fact]
    public void CliParser_Handles_Quoted_Values_With_Spaces()
    {
        var parsed = SkillExecParser.Parse(
            "notion search --query \"meeting notes for monday\"");

        Assert.NotNull(parsed);
        Assert.Equal("notion", parsed.Value.Skill);
        Assert.Equal("search", parsed.Value.Action);
        Assert.Equal("meeting notes for monday", parsed.Value.Args["query"]);
    }

    [Fact]
    public void CliParser_Handles_Single_Quoted_Values()
    {
        var parsed = SkillExecParser.Parse(
            "notion search --query 'weekly standup'");

        Assert.NotNull(parsed);
        Assert.Equal("weekly standup", parsed.Value.Args["query"]);
    }

    [Fact]
    public void CliParser_Coerces_Int_And_Bool()
    {
        var parsed = SkillExecParser.Parse(
            "github list_issues --max_results 25 --include_closed true");
        Assert.NotNull(parsed);
        var args = parsed.Value.Args;

        Assert.Equal(25, args["max_results"]);
        Assert.Equal(true, args["include_closed"]);
    }

    [Fact]
    public void CliParser_Returns_Null_For_Too_Few_Parts()
    {
        var result = SkillExecParser.Parse("onlyskill");
        Assert.Null(result);
    }

    // ── ConversationHistory Prune: multi-tool-call orphaning ──────────────

    [Fact]
    public void History_Prune_Does_Not_Orphan_Multi_ToolCall_Results()
    {
        var history = new ConversationHistory();

        // System message
        history.Push(new ChatMessage { Role = "system", Content = "You are a test agent." });

        // Old assistant with 3 tool calls
        history.Push(new ChatMessage
        {
            Role = "assistant",
            Content = "",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "call_1", Name = "skill_exec", Arguments = "{}" },
                new() { Id = "call_2", Name = "skill_exec", Arguments = "{}" },
                new() { Id = "call_3", Name = "skill_exec", Arguments = "{}" },
            }
        });
        history.Push(new ChatMessage { Role = "tool", Content = "result 1", ToolCallId = "call_1" });
        history.Push(new ChatMessage { Role = "tool", Content = "result 2", ToolCallId = "call_2" });
        history.Push(new ChatMessage { Role = "tool", Content = "result 3", ToolCallId = "call_3" });

        // Recent messages to keep
        history.Push(new ChatMessage { Role = "user", Content = "What happened?" });
        history.Push(new ChatMessage { Role = "assistant", Content = "Here's what I found." });

        history.Prune(maxTokens: 100, keepRecent: 2);

        // After pruning, every tool message must have a preceding assistant with matching tool_calls
        AssertNoOrphanedToolMessages(history);
    }

    [Fact]
    public void History_Phase2_Drop_Does_Not_Orphan_Tool_Messages()
    {
        var history = new ConversationHistory();

        history.Push(new ChatMessage { Role = "system", Content = "system" });

        // assistant with tool call
        history.Push(new ChatMessage
        {
            Role = "assistant",
            Content = "",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "call_a", Name = "shell", Arguments = "{}" },
            }
        });
        history.Push(new ChatMessage { Role = "tool", Content = new string('x', 5000), ToolCallId = "call_a" });

        // Recent
        history.Push(new ChatMessage { Role = "user", Content = "Done?" });

        history.Prune(maxTokens: 50, keepRecent: 1);

        AssertNoOrphanedToolMessages(history);
    }

    private static void AssertNoOrphanedToolMessages(ConversationHistory history)
    {
        var msgs = history.Messages;
        for (var i = 0; i < msgs.Count; i++)
        {
            if (msgs[i].Role != "tool") continue;
            var toolCallId = msgs[i].ToolCallId;
            // There must be a preceding assistant message that contains this tool call ID
            var found = false;
            for (var j = i - 1; j >= 0; j--)
            {
                if (msgs[j].Role == "assistant" && msgs[j].ToolCalls?.Any(tc => tc.Id == toolCallId) == true)
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found, $"Orphaned tool message at index {i} with ToolCallId '{toolCallId}' — no preceding assistant with matching tool_calls.");
        }
    }

    // ── Parser edge cases ─────────────────────────────────────────────────

    [Fact]
    public void CliParser_Empty_Quoted_String()
    {
        var parsed = SkillExecParser.Parse("notion search --query \"\"");
        Assert.NotNull(parsed);
        Assert.Equal("", parsed.Value.Args["query"]);
    }

    [Fact]
    public void CliParser_Escaped_Quotes_Inside_Quoted_String()
    {
        // LLMs often send: --query "he said \"hello\""
        var parsed = SkillExecParser.Parse("notion search --query \"he said \\\"hello\\\"\"");
        Assert.NotNull(parsed);
        Assert.Equal("he said \"hello\"", parsed.Value.Args["query"]);
    }

    [Fact]
    public void CliParser_Mixed_Quotes_Single_Inside_Double()
    {
        var parsed = SkillExecParser.Parse("notion search --query \"it's a test\"");
        Assert.NotNull(parsed);
        Assert.Equal("it's a test", parsed.Value.Args["query"]);
    }

    [Fact]
    public void CliParser_Json_Value_In_Single_Quotes()
    {
        var parsed = SkillExecParser.Parse("github list_issues --filter '{\"status\": \"open\"}'");
        Assert.NotNull(parsed);
        Assert.Equal("{\"status\": \"open\"}", parsed.Value.Args["filter"]);
    }

    [Fact]
    public void CliParser_Unclosed_Quote_Uses_Rest_Of_String()
    {
        // Graceful degradation: unclosed quote should capture rest of input
        var parsed = SkillExecParser.Parse("notion search --query \"unclosed value");
        Assert.NotNull(parsed);
        Assert.Equal("unclosed value", parsed.Value.Args["query"]);
    }

    [Fact]
    public void CliParser_Flag_Without_Value_At_End()
    {
        // --verbose at end with no value should not crash or eat the next arg
        var parsed = SkillExecParser.Parse("github list_issues --repo myrepo --verbose");
        Assert.NotNull(parsed);
        Assert.Equal("myrepo", parsed.Value.Args["repo"]);
        // --verbose has no value, should not appear in args (or be handled gracefully)
        Assert.False(parsed.Value.Args.ContainsKey("verbose"));
    }

    [Fact]
    public void CliParser_Value_Starting_With_Dash_Not_Treated_As_Flag()
    {
        // Negative number or ISO date with timezone offset
        var parsed = SkillExecParser.Parse("stock get_price --change -5.2");
        Assert.NotNull(parsed);
        Assert.Equal("-5.2", parsed.Value.Args["change"]);
    }

    [Fact]
    public void CliParser_Equals_Syntax()
    {
        // Some LLMs use --key=value syntax
        var parsed = SkillExecParser.Parse("github list_issues --repo=myrepo --state=open");
        Assert.NotNull(parsed);
        Assert.Equal("myrepo", parsed.Value.Args["repo"]);
        Assert.Equal("open", parsed.Value.Args["state"]);
    }

    // ── Prune edge cases ──────────────────────────────────────────────────

    [Fact]
    public void Prune_Incomplete_Tool_Results_Does_Not_Orphan()
    {
        // Assistant requested 3 tool calls but only 2 results arrived (crash mid-turn)
        var history = new ConversationHistory();
        history.Push(new ChatMessage { Role = "system", Content = "sys" });
        history.Push(new ChatMessage
        {
            Role = "assistant", Content = "",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "c1", Name = "a", Arguments = "{}" },
                new() { Id = "c2", Name = "b", Arguments = "{}" },
                new() { Id = "c3", Name = "c", Arguments = "{}" },
            }
        });
        history.Push(new ChatMessage { Role = "tool", Content = "r1", ToolCallId = "c1" });
        history.Push(new ChatMessage { Role = "tool", Content = "r2", ToolCallId = "c2" });
        // c3 result missing — user message came instead
        history.Push(new ChatMessage { Role = "user", Content = "What happened?" });
        history.Push(new ChatMessage { Role = "assistant", Content = "Sorry, something failed." });

        history.Prune(maxTokens: 50, keepRecent: 2);

        AssertNoOrphanedToolMessages(history);
    }

    [Fact]
    public void Prune_Multiple_ToolCall_Groups_Back_To_Back()
    {
        var history = new ConversationHistory();
        history.Push(new ChatMessage { Role = "system", Content = "sys" });

        // Group 1: 2 tool calls
        history.Push(new ChatMessage
        {
            Role = "assistant", Content = "",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "a1", Name = "x", Arguments = "{}" },
                new() { Id = "a2", Name = "y", Arguments = "{}" },
            }
        });
        history.Push(new ChatMessage { Role = "tool", Content = "r1", ToolCallId = "a1" });
        history.Push(new ChatMessage { Role = "tool", Content = "r2", ToolCallId = "a2" });

        // Group 2: 2 tool calls
        history.Push(new ChatMessage
        {
            Role = "assistant", Content = "",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "b1", Name = "x", Arguments = "{}" },
                new() { Id = "b2", Name = "y", Arguments = "{}" },
            }
        });
        history.Push(new ChatMessage { Role = "tool", Content = "r3", ToolCallId = "b1" });
        history.Push(new ChatMessage { Role = "tool", Content = "r4", ToolCallId = "b2" });

        // Recent
        history.Push(new ChatMessage { Role = "user", Content = "done" });

        history.Prune(maxTokens: 50, keepRecent: 1);

        AssertNoOrphanedToolMessages(history);
    }

    [Fact]
    public void Prune_Phase2_Only_System_And_Tool_Does_Not_Infinite_Loop()
    {
        // Pathological: system + orphaned tool (shouldn't happen, but must not infinite loop)
        var history = new ConversationHistory();
        history.Push(new ChatMessage { Role = "system", Content = new string('x', 5000) });
        history.Push(new ChatMessage { Role = "tool", Content = new string('y', 5000), ToolCallId = "orphan" });

        // Should terminate, not loop forever
        history.Prune(maxTokens: 10, keepRecent: 0);

        // The orphaned tool should be dropped since it has no parent
        Assert.True(history.Count <= 2);
    }

    [Fact]
    public void Prune_Protected_ToolCalls_Not_Touched()
    {
        var history = new ConversationHistory();
        history.Push(new ChatMessage { Role = "system", Content = "sys" });
        history.Push(new ChatMessage
        {
            Role = "assistant", Content = "",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "c1", Name = "shell", Arguments = "{}" },
                new() { Id = "c2", Name = "shell", Arguments = "{}" },
            }
        });
        history.Push(new ChatMessage { Role = "tool", Content = "result1", ToolCallId = "c1" });
        history.Push(new ChatMessage { Role = "tool", Content = "result2", ToolCallId = "c2" });

        // keepRecent=4 means all 4 non-system messages are protected
        history.Prune(maxTokens: 50, keepRecent: 4);

        // assistant with tool_calls should still be intact
        var assistant = history.Messages.First(m => m.Role == "assistant");
        Assert.NotNull(assistant.ToolCalls);
        Assert.Equal(2, assistant.ToolCalls.Count);
        AssertNoOrphanedToolMessages(history);
    }
}
