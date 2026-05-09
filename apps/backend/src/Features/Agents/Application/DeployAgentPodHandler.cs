using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents.Handlers;

internal sealed class DeployAgentPodHandler : INotificationHandler<AgentCreatedEvent>
{
    private readonly IAgentDeployer _deployer;
    private readonly IAgentRepository _agentRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<DeployAgentPodHandler> _logger;

    public DeployAgentPodHandler(
        IAgentDeployer deployer,
        IAgentRepository agentRepository,
        IPublisher publisher,
        ILogger<DeployAgentPodHandler> logger)
    {
        _deployer = deployer;
        _agentRepository = agentRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        var record = await _agentRepository.GetByAsync(new AgentFilter { Id = notification.AgentId }, ct);
        if (record is null) return;

        try
        {
            var deployment = await _deployer.DeployAsync(notification.AgentId, ct);
            record.MarkDeployed(deployment.PodName, deployment.ServiceUrl);
            await _agentRepository.UpdateAsync(record, ct);
            _logger.LogInformation("Agent {AgentId} deployed as pod {PodName}", notification.AgentId, record.PodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy agent {AgentId}", notification.AgentId);
            record.MarkFailed();
            await _agentRepository.UpdateAsync(record, ct);
            await _publisher.Publish(new AgentErrorOccurredEvent(
                notification.AgentId,
                Guid.NewGuid().ToString("N"),
                $"Failed to deploy agent runtime: {ex.Message}"), ct);
        }
    }
}
