using MediatR;

namespace EnterpriseAgentOs.Application.Features.Analytics;

internal sealed class TurnLoggingHandler :
    INotificationHandler<TurnStartedEvent>,
    INotificationHandler<TurnCompletedEvent>,
    INotificationHandler<PodConnectedEvent>,
    INotificationHandler<LlmCallCompletedEvent>,
    INotificationHandler<ToolCallStartedEvent>,
    INotificationHandler<ToolCallCompletedEvent>,
    INotificationHandler<AgentErrorOccurredEvent>,
    INotificationHandler<MessageOutEvent>
{
    private readonly IAgentLogService _logService;

    public TurnLoggingHandler(IAgentLogService logService) => _logService = logService;

    public async Task Handle(TurnStartedEvent e, CancellationToken ct)
    {
        var preview = e.UserMessage.Length > 100 ? e.UserMessage[..100] + "..." : e.UserMessage;
        await _logService.AppendAsync(
            AgentLogRecord.System(e.AgentId, $"Turn started: {preview}", e.CorrelationId, e.OccurredAt), ct);
    }

    public async Task Handle(TurnCompletedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            AgentLogRecord.System(e.AgentId,
                $"Turn complete: {e.Iterations} iterations, {e.ToolCallCount} tool calls",
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs)), ct);
    }

    public async Task Handle(PodConnectedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            AgentLogRecord.System(e.AgentId, "Pod connected",
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs)), ct);
    }

    public async Task Handle(LlmCallCompletedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            AgentLogRecord.System(e.AgentId,
                $"LLM call complete ({e.InputTokens ?? 0} in, {e.OutputTokens ?? 0} out)",
                e.CorrelationId, e.OccurredAt, new TokenUsage(e.InputTokens, e.OutputTokens, e.DurationMs)), ct);
    }

    public async Task Handle(ToolCallStartedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            AgentLogRecord.ToolCallEntry(e.AgentId, e.ToolName, e.ArgsJson, e.CorrelationId, e.OccurredAt), ct);
    }

    public async Task Handle(ToolCallCompletedEvent e, CancellationToken ct)
    {
        var content = e.Output.Length > 10000 ? e.Output[..10000] + "\n[truncated]" : e.Output;
        if (!e.Success) content = $"[failed] {content}";
        await _logService.AppendAsync(
            AgentLogRecord.ToolResultEntry(e.AgentId, e.ToolName, content,
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs)), ct);
    }

    public async Task Handle(AgentErrorOccurredEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            AgentLogRecord.Error(e.AgentId, e.Message, e.CorrelationId, e.OccurredAt), ct);
    }

    public async Task Handle(MessageOutEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(
            AgentLogRecord.MessageOut(e.AgentId, e.Content, e.CorrelationId, e.OccurredAt), ct);
    }
}
