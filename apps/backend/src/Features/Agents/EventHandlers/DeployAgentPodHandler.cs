namespace OffceOs.EventHandlers.Features.Agents;

internal sealed class DeployAgentPodHandler : INotificationHandler<AgentCreatedEvent>
{
    private readonly ILogger<DeployAgentPodHandler> _logger;

    public DeployAgentPodHandler(ILogger<DeployAgentPodHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        _ = ct;
        _logger.LogInformation("Agent {AgentId} registered for resource API execution; no backend harness pod is deployed.", notification.AgentId);
        return Task.CompletedTask;
    }
}
