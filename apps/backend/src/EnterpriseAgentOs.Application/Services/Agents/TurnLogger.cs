namespace EnterpriseAgentOs.Application.Services.Agents;

/// <summary>
/// Structured event logger for a single agent turn.
/// Emits exactly one AgentLogRecord per significant state transition.
/// No heartbeats, no repeated events — each method fires once.
///
/// The delegate pattern allows both real (IAgentLogService.AppendAsync) and
/// test (in-memory list) usage without mocking infrastructure.
/// </summary>
public sealed class TurnLogger
{
    private readonly Guid _agentId;
    private readonly string _correlationId;
    private readonly Action<AgentLogRecord> _emit;

    public TurnLogger(Guid agentId, string correlationId, Action<AgentLogRecord> emit)
    {
        _agentId = agentId;
        _correlationId = correlationId;
        _emit = emit;
    }

    public void TurnStart(string userMessage)
    {
        var preview = userMessage.Length > 100 ? userMessage[..100] + "..." : userMessage;
        Emit(AgentLogType.System, $"Turn started: {preview}");
    }

    public void PodConnected(int durationMs)
    {
        Emit(AgentLogType.System, "Pod connected", durationMs: durationMs);
    }

    public void LlmCallComplete(int durationMs, int? inputTokens = null, int? outputTokens = null)
    {
        Emit(AgentLogType.System, $"LLM call complete ({inputTokens ?? 0} in, {outputTokens ?? 0} out)",
            durationMs: durationMs, inputTokens: inputTokens, outputTokens: outputTokens);
    }

    public void ToolCallStart(string tool, string argsJson)
    {
        Emit(AgentLogType.ToolCall, argsJson, tool: tool);
    }

    public void ToolCallResult(string tool, bool success, string output, int durationMs)
    {
        var content = output.Length > 10000 ? output[..10000] + "\n[truncated]" : output;
        if (!success) content = $"[failed] {content}";
        Emit(AgentLogType.ToolResult, content, tool: tool, durationMs: durationMs);
    }

    public void MessageOut(string content)
    {
        Emit(AgentLogType.MessageOut, content);
    }

    public void TurnComplete(int totalDurationMs, int iterations, int toolCallCount)
    {
        Emit(AgentLogType.System,
            $"Turn complete: {iterations} iterations, {toolCallCount} tool calls",
            durationMs: totalDurationMs);
    }

    public void Error(string message)
    {
        Emit(AgentLogType.Error, message);
    }

    public void Error(AgentError agentError)
    {
        var record = agentError.ToLogRecord(_agentId);
        record.CorrelationId = _correlationId;
        _emit(record);
    }

    private void Emit(AgentLogType type, string content, string? tool = null,
        int? durationMs = null, int? inputTokens = null, int? outputTokens = null)
    {
        _emit(new AgentLogRecord
        {
            AgentId = _agentId,
            Type = type,
            Content = content,
            Tool = tool,
            DurationMs = durationMs,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CorrelationId = _correlationId,
            Time = DateTime.UtcNow,
        });
    }
}
