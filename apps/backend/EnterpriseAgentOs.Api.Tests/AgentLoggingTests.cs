using System.Text.Json;

namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// Tests that the agent turn loop logs all significant events with timing.
/// Every state transition should produce exactly one log entry.
/// No heartbeats, no repeated waiting events.
/// </summary>
public class AgentLoggingTests
{
    /// <summary>
    /// A TurnLogger should emit TURN_START at the beginning of a turn with the user message.
    /// </summary>
    [Fact]
    public void TurnLogger_Emits_TurnStart()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.TurnStart("list my notion pages");

        Assert.Single(logs);
        Assert.Equal(AgentLogType.System, logs[0].Type);
        Assert.Contains("Turn started", logs[0].Content);
        Assert.Contains("list my notion pages", logs[0].Content);
    }

    /// <summary>
    /// Pod connection should log with duration.
    /// </summary>
    [Fact]
    public void TurnLogger_Emits_PodConnected_With_Duration()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.PodConnected(durationMs: 230);

        Assert.Single(logs);
        Assert.Equal(AgentLogType.System, logs[0].Type);
        Assert.Equal(230, logs[0].DurationMs);
        Assert.Contains("Pod connected", logs[0].Content);
    }

    /// <summary>
    /// LLM call should log with duration and token counts.
    /// </summary>
    [Fact]
    public void TurnLogger_Emits_LlmCall_With_Duration_And_Tokens()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.LlmCallComplete(durationMs: 1500, inputTokens: 2048, outputTokens: 512);

        Assert.Single(logs);
        Assert.Equal(AgentLogType.System, logs[0].Type);
        Assert.Equal(1500, logs[0].DurationMs);
        Assert.Equal(2048, logs[0].InputTokens);
        Assert.Equal(512, logs[0].OutputTokens);
        Assert.Contains("LLM", logs[0].Content);
    }

    /// <summary>
    /// Tool call should log tool name and duration.
    /// </summary>
    [Fact]
    public void TurnLogger_Emits_ToolCall_With_Duration()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.ToolCallStart("shell", "{\"command\":\"ls\"}");
        logger.ToolCallResult("shell", success: true, output: "file1.txt\nfile2.txt", durationMs: 45);

        Assert.Equal(2, logs.Count);
        Assert.Equal(AgentLogType.ToolCall, logs[0].Type);
        Assert.Equal("shell", logs[0].Tool);
        Assert.Equal(AgentLogType.ToolResult, logs[1].Type);
        Assert.Equal(45, logs[1].DurationMs);
    }

    /// <summary>
    /// Turn complete should log total duration and iteration count.
    /// </summary>
    [Fact]
    public void TurnLogger_Emits_TurnComplete_With_Stats()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.TurnComplete(totalDurationMs: 5400, iterations: 3, toolCallCount: 5);

        Assert.Single(logs);
        Assert.Equal(AgentLogType.System, logs[0].Type);
        Assert.Equal(5400, logs[0].DurationMs);
        Assert.Contains("3 iterations", logs[0].Content);
        Assert.Contains("5 tool calls", logs[0].Content);
    }

    /// <summary>
    /// Error should log with context about what was happening.
    /// </summary>
    [Fact]
    public void TurnLogger_Emits_Error_With_Context()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.Error("Pod connection failed: timeout after 10s");

        Assert.Single(logs);
        Assert.Equal(AgentLogType.Error, logs[0].Type);
        Assert.Contains("timeout", logs[0].Content);
    }

    /// <summary>
    /// Each event fires exactly once. No duplicate events for the same action.
    /// </summary>
    [Fact]
    public void TurnLogger_No_Duplicate_Events()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.TurnStart("hello");
        logger.PodConnected(100);
        logger.LlmCallComplete(1000, 500, 200);
        logger.ToolCallStart("shell", "{\"command\":\"echo hi\"}");
        logger.ToolCallResult("shell", true, "hi", 30);
        logger.LlmCallComplete(800, 600, 100);
        logger.TurnComplete(3000, 2, 1);

        // Exactly 7 events — one per action, no repeats
        Assert.Equal(7, logs.Count);
    }

    /// <summary>
    /// All events carry the same correlationId for the turn.
    /// </summary>
    [Fact]
    public void TurnLogger_All_Events_Share_CorrelationId()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "test-corr-123", record => logs.Add(record));

        logger.TurnStart("hello");
        logger.PodConnected(50);
        logger.TurnComplete(100, 1, 0);

        Assert.All(logs, log => Assert.Equal("test-corr-123", log.CorrelationId));
    }

    /// <summary>
    /// MessageOut is separate from TurnComplete — the assistant text is logged as MessageOut.
    /// </summary>
    [Fact]
    public void TurnLogger_Emits_MessageOut()
    {
        var logs = new List<AgentLogRecord>();
        var logger = new TurnLogger(Guid.NewGuid(), "corr-1", record => logs.Add(record));

        logger.MessageOut("Here are your Notion pages: ...");

        Assert.Single(logs);
        Assert.Equal(AgentLogType.MessageOut, logs[0].Type);
        Assert.Contains("Notion pages", logs[0].Content);
    }
}
