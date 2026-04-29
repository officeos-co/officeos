using MediatR;

namespace EnterpriseAgentOs.Application.Features.AgentLogs.Handlers;

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
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.System, Content = $"Turn started: {preview}",
            CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }

    public async Task Handle(TurnCompletedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.System,
            Content = $"Turn complete: {e.Iterations} iterations, {e.ToolCallCount} tool calls",
            Usage = new TokenUsage(null, null, e.DurationMs), CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }

    public async Task Handle(PodConnectedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.System, Content = "Pod connected",
            Usage = new TokenUsage(null, null, e.DurationMs), CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }

    public async Task Handle(LlmCallCompletedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.System,
            Content = $"LLM call complete ({e.InputTokens ?? 0} in, {e.OutputTokens ?? 0} out)",
            Usage = new TokenUsage(e.InputTokens, e.OutputTokens, e.DurationMs),
            CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }

    public async Task Handle(ToolCallStartedEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.ToolCall, Tool = e.ToolName,
            Content = e.ArgsJson, CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }

    public async Task Handle(ToolCallCompletedEvent e, CancellationToken ct)
    {
        var content = e.Output.Length > 10000 ? e.Output[..10000] + "\n[truncated]" : e.Output;
        if (!e.Success) content = $"[failed] {content}";
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.ToolResult, Tool = e.ToolName,
            Content = content, Usage = new TokenUsage(null, null, e.DurationMs),
            CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }

    public async Task Handle(AgentErrorOccurredEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.Error, Content = e.Message,
            CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }

    public async Task Handle(MessageOutEvent e, CancellationToken ct)
    {
        await _logService.AppendAsync(new AgentLogRecord
        {
            AgentId = e.AgentId, Type = AgentLogType.MessageOut, Content = e.Content,
            CorrelationId = e.CorrelationId, Time = e.OccurredAt,
        }, ct);
    }
}
