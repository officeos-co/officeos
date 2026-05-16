using OffceOs.Domain.Features.AgentHarness;
using OffceOs.Domain.Features.Agents;
namespace OffceOs.EventHandlers.Features.Agents;

internal sealed class RemoveAgentPodHandler : INotificationHandler<AgentDeletedEvent>
{
    private readonly IAgentDeployer _agentDeployer;

    public RemoveAgentPodHandler(IAgentDeployer deployer)
        => _agentDeployer = deployer;

    public async Task Handle(AgentDeletedEvent notification, CancellationToken ct)
    {
        if (!notification.HasPod || string.IsNullOrEmpty(notification.PodName))
            return;

        await _agentDeployer.RemoveAsync(notification.PodName, ct);
    }
}
