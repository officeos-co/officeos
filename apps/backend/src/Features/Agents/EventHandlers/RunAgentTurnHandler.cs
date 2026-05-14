namespace OffceOs.EventHandlers.Features.Agents;

internal sealed class RunAgentTurnHandler : INotificationHandler<MessageReceivedEvent>
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly ILogger<RunAgentTurnHandler> _logger;

    public RunAgentTurnHandler(
        IAgentRepository agentRepository,
        IAgentRunRepository agentRunRepository,
        ILogger<RunAgentTurnHandler> logger)
    {
        _agentRepository = agentRepository;
        _agentRunRepository = agentRunRepository;
        _logger = logger;
    }

    public async Task Handle(MessageReceivedEvent notification, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = notification.AgentId }, ct);
        if (agent is null)
        {
            _logger.LogWarning("Ignoring message for missing agent {AgentId}", notification.AgentId);
            return;
        }

        await _agentRunRepository.CreateAsync(new AgentRunRecord
        {
            AgentId = notification.AgentId,
            WorkspaceId = agent.WorkspaceId,
            ParentCorrelationId = notification.CorrelationId,
            Kind = "opencode",
            Status = "queued",
            Name = agent.Name,
            Description = "opencode",
            Prompt = notification.Content,
        }, ct);
    }
}
