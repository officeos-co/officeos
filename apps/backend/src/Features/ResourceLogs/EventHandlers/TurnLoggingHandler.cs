using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Features.AgentHarness;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.ResourceLogs;
namespace OffceOs.EventHandlers.Features.ResourceLogs;

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
    private readonly IResourceLogService _resourceLogService;

    public TurnLoggingHandler(IResourceLogService logService) => _resourceLogService = logService;

    public async Task Handle(TurnStartedEvent e, CancellationToken ct)
    {
        var preview = e.UserMessage.Length > 100 ? e.UserMessage[..100] + "..." : e.UserMessage;
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.System(e.AgentId, $"Turn started: {preview}", e.CorrelationId, e.OccurredAt), ct);
    }

    public async Task Handle(TurnCompletedEvent e, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.System(e.AgentId,
                $"Turn complete: {e.Iterations} iterations, {e.ToolCallCount} tool calls",
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs)), ct);
    }

    public Task Handle(PodConnectedEvent e, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public async Task Handle(TurnDiagnosticEvent e, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.System(e.AgentId, e.Message,
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs)), ct);
    }

    public async Task Handle(LlmCallCompletedEvent e, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(
            new ResourceLogRecord
            {
                AgentId = e.AgentId,
                Type = ResourceLogType.ModelCall,
                Tool = e.Model,
                Content = $"LLM call complete: {e.Model} ({e.InputTokens ?? 0} in, {e.OutputTokens ?? 0} out)",
                CorrelationId = e.CorrelationId,
                Time = e.OccurredAt,
                Usage = new TokenUsage(e.InputTokens, e.OutputTokens, e.DurationMs),
                MetadataJson = JsonSerializer.Serialize(new
                {
                    e.Provider,
                    e.Model,
                    e.InputTokens,
                    e.OutputTokens,
                    e.CacheReadTokens,
                    e.CacheWriteTokens,
                    e.ReasoningTokens,
                    e.EstimatedTokens,
                    e.DurationMs,
                }),
            }, ct);
    }

    public async Task Handle(ToolCallStartedEvent e, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.ToolCallEntry(e.AgentId, e.ToolName, e.ArgsJson, e.CorrelationId, e.OccurredAt), ct);
    }

    public async Task Handle(ToolCallCompletedEvent e, CancellationToken ct)
    {
        var content = e.Output.Length > 10000 ? e.Output[..10000] + "\n[truncated]" : e.Output;
        if (!e.Success) content = $"[failed] {content}";
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.ToolResultEntry(e.AgentId, e.ToolName, content,
                e.CorrelationId, e.OccurredAt, new TokenUsage(null, null, e.DurationMs)), ct);
    }

    public async Task Handle(AgentErrorOccurredEvent e, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.Error(e.AgentId, e.Message, e.CorrelationId, e.OccurredAt), ct);
    }

    public async Task Handle(MessageOutEvent e, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.MessageOut(e.AgentId, e.Content, e.CorrelationId, e.OccurredAt), ct);
    }

    public async Task Handle(ConversationCompactedEvent e, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(
            ResourceLogRecord.System(e.AgentId,
                $"Conversation compacted through log {e.LastCompactedLogId:N} ({e.PreCompactTokens} -> {e.PostCompactTokens} estimated tokens)",
                e.CorrelationId, e.OccurredAt), ct);
    }
}
