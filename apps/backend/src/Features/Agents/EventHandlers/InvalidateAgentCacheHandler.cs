using MediatR;

namespace EnterpriseAgentOs.EventHandlers.Features.Agents;

internal sealed class InvalidateAgentCacheHandler :
    INotificationHandler<AgentCreatedEvent>,
    INotificationHandler<AgentDeletedEvent>,
    INotificationHandler<AgentUpdatedEvent>
{
    private readonly IDistributedCache _cache;
    private readonly IAgentRepository _agentRepository;

    public InvalidateAgentCacheHandler(IDistributedCache cache, IAgentRepository agentRepository)
    {
        _cache = cache;
        _agentRepository = agentRepository;
    }

    public async Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        await AgentCacheKeys.InvalidateAgentAsync(_cache, notification.AgentId, notification.OwnerId, ct);
    }

    public async Task Handle(AgentDeletedEvent notification, CancellationToken ct)
    {
        await AgentCacheKeys.InvalidateAgentAsync(_cache, notification.AgentId, notification.OwnerId, ct);
    }

    public async Task Handle(AgentUpdatedEvent notification, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = notification.AgentId }, ct);
        await AgentCacheKeys.InvalidateAgentAsync(_cache, notification.AgentId, agent?.OwnerId, ct);
    }
}
