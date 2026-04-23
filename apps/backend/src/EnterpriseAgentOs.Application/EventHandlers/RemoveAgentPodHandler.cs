using MediatR;

namespace EnterpriseAgentOs.Application.EventHandlers;

internal sealed class RemoveAgentPodHandler : INotificationHandler<AgentDeletedEvent>
{
    private readonly IAgentDeployer _deployer;
    private readonly ILogger<RemoveAgentPodHandler> _logger;

    public RemoveAgentPodHandler(IAgentDeployer deployer, ILogger<RemoveAgentPodHandler> logger)
    {
        _deployer = deployer;
        _logger = logger;
    }

    public async Task Handle(AgentDeletedEvent notification, CancellationToken ct)
    {
        if (!notification.HasPod || string.IsNullOrEmpty(notification.PodName))
            return;

        _logger.LogInformation("Removing pod {PodName} for agent {AgentId}", notification.PodName, notification.AgentId);
        await _deployer.RemoveAsync(notification.PodName, ct);
    }
}
