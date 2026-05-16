namespace OffceOs.EventHandlers.Features.Agents;

internal sealed class DeployAgentPodHandler : INotificationHandler<AgentCreatedEvent>
{
    public Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        _ = notification;
        _ = ct;
        return Task.CompletedTask;
    }
}
