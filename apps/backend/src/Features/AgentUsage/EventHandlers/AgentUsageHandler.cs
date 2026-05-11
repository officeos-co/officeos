namespace OffceOs.EventHandlers.Features.AgentUsage;

internal sealed class AgentUsageHandler : INotificationHandler<LlmCallCompletedEvent>
{
    private readonly IAgentUsageService _agentUsageService;

    public AgentUsageHandler(IAgentUsageService agentUsageService)
    {
        _agentUsageService = agentUsageService;
    }

    public async Task Handle(LlmCallCompletedEvent notification, CancellationToken ct)
    {
        var usage = new AgentUsageResolutionResult(
            notification.InputTokens ?? 0,
            notification.OutputTokens ?? 0,
            notification.CacheReadTokens,
            notification.CacheWriteTokens,
            notification.ReasoningTokens,
            notification.EstimatedTokens,
            notification.Activity,
            (notification.ContextParts ?? [])
                .Select(part => new AgentUsageContextPartRecord
                {
                    Kind = part.Kind,
                    Label = part.Label,
                    Role = part.Role,
                    Tool = part.Tool,
                    Integration = part.Integration,
                    Tokens = part.Tokens,
                    EstimatedTokens = part.EstimatedTokens,
                    CharacterCount = part.CharacterCount,
                })
                .ToList());

        await _agentUsageService.RecordCallAsync(new AgentUsageRecordRequest(
            notification.AgentId,
            notification.CorrelationId,
            notification.Provider,
            notification.Model,
            notification.DurationMs,
            usage,
            notification.RunId,
            notification.ParentRunId), ct);
    }
}
