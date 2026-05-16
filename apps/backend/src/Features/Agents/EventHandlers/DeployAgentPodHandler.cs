using OffceOs.Features.Agents.Domain;

namespace OffceOs.Features.Agents.EventHandlers;

internal sealed class DeployAgentPodHandler : INotificationHandler<AgentCreatedEvent>
{
    public Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        _ = notification;
        _ = ct;
        return Task.CompletedTask;
    }
}
