
namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// Tests for session management: AgentSessionRecord domain model,
/// session lifecycle, context pruning, and bootstrap message injection.
/// </summary>
public class SessionTests
{
    // ── AgentSessionRecord domain model ────────────────────────────────────

    [Fact]
    public void Session_Create_Sets_Active_Status()
    {
        var agentId = Guid.NewGuid();
        var session = AgentSessionRecord.Create(agentId);

        Assert.Equal(agentId, session.AgentId);
        Assert.True(session.IsActive);
        Assert.Equal("active", session.Status);
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void Session_End_Sets_Ended_Status()
    {
        var session = AgentSessionRecord.Create(Guid.NewGuid());

        session.End();

        Assert.False(session.IsActive);
        Assert.Equal("ended", session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void Session_End_Idempotent()
    {
        var session = AgentSessionRecord.Create(Guid.NewGuid());
        session.End();
        var endedAt = session.EndedAt;

        session.End(); // second call

        Assert.Equal(endedAt, session.EndedAt); // unchanged
    }

    [Fact]
    public void Session_IsExpired_After_Idle_Timeout()
    {
        var session = AgentSessionRecord.Create(Guid.NewGuid());
        // Simulate last activity 2 hours ago
        session.RecordActivity(DateTime.UtcNow.AddHours(-2));

        Assert.True(session.IsExpired(idleTimeout: TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Session_IsExpired_False_When_Recent()
    {
        var session = AgentSessionRecord.Create(Guid.NewGuid());
        session.RecordActivity(DateTime.UtcNow);

        Assert.False(session.IsExpired(idleTimeout: TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Session_RecordActivity_Updates_LastActivityAt()
    {
        var session = AgentSessionRecord.Create(Guid.NewGuid());
        var before = session.LastActivityAt;

        session.RecordActivity(DateTime.UtcNow.AddMinutes(5));

        Assert.True(session.LastActivityAt > before);
    }

    [Fact]
    public void Session_MessageCount_Tracks_Messages()
    {
        var session = AgentSessionRecord.Create(Guid.NewGuid());
        Assert.Equal(0, session.MessageCount);

        session.IncrementMessageCount();
        session.IncrementMessageCount();

        Assert.Equal(2, session.MessageCount);
    }

    // ── Context pruning (OpenClaw-style tool result trimming) ──────────────

    [Fact]
    public void ContextPruning_SoftTrims_Old_Tool_Results()
    {
        var history = new ConversationHistory();

        // Push an old tool pair with a very long result
        history.Push(new ChatMessage
        {
            Role = "assistant",
            Content = "Let me check",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "call_old", Name = "shell", Arguments = "{\"command\":\"ls\"}" }
            }
        });
        history.Push(new ChatMessage
        {
            Role = "tool",
            Content = new string('x', 2000), // Large tool result
            ToolCallId = "call_old"
        });

        // Push recent messages to make the tool pair "old"
        for (var i = 0; i < 6; i++)
            history.Push(new ChatMessage { Role = "user", Content = $"msg {i}" });

        history.PruneToolResults(maxResultChars: 200, keepRecentTurns: 4);

        // The old tool result should be soft-trimmed
        var toolMsg = history.Messages.FirstOrDefault(m => m.Role == "tool" && m.ToolCallId == "call_old");
        Assert.NotNull(toolMsg);
        Assert.True(toolMsg.Content!.Length < 2000);
        Assert.Contains("...", toolMsg.Content);
    }

    [Fact]
    public void ContextPruning_Preserves_Recent_Tool_Results()
    {
        var history = new ConversationHistory();

        // Push a recent tool pair
        history.Push(new ChatMessage
        {
            Role = "assistant",
            Content = "checking",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "call_recent", Name = "shell", Arguments = "{}" }
            }
        });
        history.Push(new ChatMessage
        {
            Role = "tool",
            Content = new string('y', 2000),
            ToolCallId = "call_recent"
        });

        history.PruneToolResults(maxResultChars: 200, keepRecentTurns: 4);

        // Recent results are preserved
        var toolMsg = history.Messages.First(m => m.Role == "tool");
        Assert.Equal(2000, toolMsg.Content!.Length);
    }

    [Fact]
    public void ContextPruning_HardClears_Very_Old_Results()
    {
        var history = new ConversationHistory();

        // Push old tool pair
        history.Push(new ChatMessage
        {
            Role = "assistant",
            Content = "old check",
            ToolCalls = new List<ChatToolCall>
            {
                new() { Id = "call_ancient", Name = "shell", Arguments = "{}" }
            }
        });
        history.Push(new ChatMessage
        {
            Role = "tool",
            Content = new string('z', 500),
            ToolCallId = "call_ancient"
        });

        // Push many messages to age out the tool pair
        for (var i = 0; i < 12; i++)
            history.Push(new ChatMessage { Role = "user", Content = $"msg {i}" });

        history.PruneToolResults(maxResultChars: 200, keepRecentTurns: 4, hardClearAfterTurns: 8);

        // Very old tool results should be hard-cleared to placeholder
        var toolMsg = history.Messages.FirstOrDefault(m => m.Role == "tool" && m.ToolCallId == "call_ancient");
        Assert.NotNull(toolMsg);
        Assert.Equal("[tool result cleared]", toolMsg.Content);
    }

    // ── Bootstrap message ──────────────────────────────────────────────────

    [Fact]
    public void Session_FormatBootstrapMessage_Uses_BootstrapFile()
    {
        var agentId = Guid.NewGuid();
        var files = new List<AgentPersonalityRecord>
        {
            AgentPersonalityRecord.Create(agentId, "BOOTSTRAP.md", "Welcome! Ask the user their name and preferred working style."),
            AgentPersonalityRecord.Create(agentId, "SOUL.md", "Be helpful."),
        };

        var session = AgentSessionRecord.Create(agentId);
        var bootstrap = session.FormatBootstrapMessage(files);

        Assert.NotNull(bootstrap);
        Assert.Contains("Ask the user their name", bootstrap);
    }

    [Fact]
    public void Session_FormatBootstrapMessage_Returns_Default_When_No_BootstrapFile()
    {
        var agentId = Guid.NewGuid();
        var files = new List<AgentPersonalityRecord>
        {
            AgentPersonalityRecord.Create(agentId, "SOUL.md", "Be helpful."),
        };

        var session = AgentSessionRecord.Create(agentId);
        var bootstrap = session.FormatBootstrapMessage(files);

        Assert.NotNull(bootstrap);
        Assert.Contains("Session started", bootstrap);
    }

    [Fact]
    public void Session_FormatBootstrapMessage_Null_For_NonFirst_Session()
    {
        var agentId = Guid.NewGuid();
        var files = new List<AgentPersonalityRecord>
        {
            AgentPersonalityRecord.Create(agentId, "BOOTSTRAP.md", "First-run setup."),
        };

        var session = AgentSessionRecord.Create(agentId);
        // Simulate this is not the first session
        var bootstrap = session.FormatBootstrapMessage(files, isFirstSession: false);

        Assert.NotNull(bootstrap);
        Assert.Contains("New session started", bootstrap);
        Assert.DoesNotContain("First-run setup", bootstrap);
    }
}
