namespace OffceOs.EventHandlers.Features.Agents;

internal sealed class QueueAgentWorkHandler : INotificationHandler<MessageReceivedEvent>
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentLogService _agentLogService;
    private readonly ILogger<QueueAgentWorkHandler> _logger;

    public QueueAgentWorkHandler(
        IAgentRepository agentRepository,
        IAgentLogService agentLogService,
        ILogger<QueueAgentWorkHandler> logger)
    {
        _agentRepository = agentRepository;
        _agentLogService = agentLogService;
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

        await _agentLogService.QueueWorkAsync(new QueueAgentWorkRequest(
            notification.AgentId,
            agent.WorkspaceId,
            notification.Content,
            notification.CorrelationId,
            AgentWorkPurposeKinds.Normalize(notification.Purpose),
            notification.DefinitionId), ct);
    }
}
