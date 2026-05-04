using MediatR;
using EnterpriseAgentOs.Application.Features.Agents;

namespace EnterpriseAgentOs.Application.Features.Analytics;

internal sealed class TurnLoggingHandler :
    INotificationHandler<TurnStartedEvent>,
    INotificationHandler<TurnCompletedEvent>,
    INotificationHandler<TurnDiagnosticEvent>,
    INotificationHandler<PodConnectedEvent>,
    INotificationHandler<LlmCallCompletedEvent>,
    INotificationHandler<ToolCallStartedEvent>,
    INotificationHandler<ToolCallCompletedEvent>,
    INotificationHandler<AgentErrorOccurredEvent>,
    INotificationHandler<MessageOutEvent>,
    INotificationHandler<ConversationCompactedEvent>
{
    private readonly IAgentLogService _logService;

    public TurnLoggingHandler(IAgentLogService logService) => _logService = logService;

    public async Task Handle(TurnStartedEvent e, CancellationToken ct)
    {
        var preview = e.UserMessage.Length > 100 ? e.UserMessage[..100] + "..." : e.UserMessage;
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.System(e.AgentId, $"Turn started: {preview}", e.CorrelationId, e.OccurredAt)), ct);
    }

    public async Task Handle(TurnCompletedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.System(e.AgentId,
                $"Turn complete: {e.Iterations} iterations, {e.ToolCallCount} tool calls",
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs))), ct);
    }

    public async Task Handle(PodConnectedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.System(e.AgentId, "Pod connected",
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs))), ct);
    }

    public async Task Handle(TurnDiagnosticEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.System(e.AgentId, e.Message,
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs))), ct);
    }

    public async Task Handle(LlmCallCompletedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.System(e.AgentId,
                $"LLM call complete ({e.InputTokens ?? 0} in, {e.OutputTokens ?? 0} out)",
                e.CorrelationId, e.OccurredAt, new TokenUsage(e.InputTokens, e.OutputTokens, e.DurationMs))), ct);
    }

    public async Task Handle(ToolCallStartedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.ToolCallEntry(e.AgentId, e.ToolName, e.ArgsJson, e.CorrelationId, e.OccurredAt)), ct);
    }

    public async Task Handle(ToolCallCompletedEvent e, CancellationToken ct)
    {
        var content = e.Output.Length > 10000 ? e.Output[..10000] + "\n[truncated]" : e.Output;
        if (!e.Success) content = $"[failed] {content}";
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.ToolResultEntry(e.AgentId, e.ToolName, content,
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs))), ct);
    }

    public async Task Handle(AgentErrorOccurredEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.Error(e.AgentId, e.Message, e.CorrelationId, e.OccurredAt)), ct);
    }

    public async Task Handle(MessageOutEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.MessageOut(e.AgentId, e.Content, e.CorrelationId, e.OccurredAt)), ct);
    }

    public async Task Handle(ConversationCompactedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            WithRun(AgentLogRecord.System(e.AgentId,
                $"Conversation compacted through log {e.LastCompactedLogId:N} ({e.PreCompactTokens} -> {e.PostCompactTokens} estimated tokens)",
                e.CorrelationId, e.OccurredAt)), ct);
    }

    private static AgentLogRecord WithRun(AgentLogRecord record) => new()
    {
        Id = record.Id,
        AgentId = record.AgentId,
        Time = record.Time,
        Type = record.Type,
        Tool = record.Tool,
        Integration = record.Integration,
        Channel = record.Channel,
        Content = record.Content,
        Usage = record.Usage,
        CorrelationId = record.CorrelationId,
        RunId = AgentRunContext.RunId,
        ParentRunId = AgentRunContext.ParentRunId,
    };
}
