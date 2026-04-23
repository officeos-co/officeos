using MediatR;

namespace EnterpriseAgentOs.Application.EventHandlers;

internal sealed class InvalidateAgentCacheHandler :
    INotificationHandler<AgentCreatedEvent>,
    INotificationHandler<AgentDeletedEvent>,
    INotificationHandler<AgentUpdatedEvent>
{
    private readonly IMemoryCache _cache;
    private const string AgentListCacheKey = "agents:list";
    private static string AgentCacheKey(Guid id) => $"agents:{id}";

    public InvalidateAgentCacheHandler(IMemoryCache cache) => _cache = cache;

    public Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        Invalidate(notification.AgentId);
        return Task.CompletedTask;
    }

    public Task Handle(AgentDeletedEvent notification, CancellationToken ct)
    {
        Invalidate(notification.AgentId);
        return Task.CompletedTask;
    }

    public Task Handle(AgentUpdatedEvent notification, CancellationToken ct)
    {
        Invalidate(notification.AgentId);
        return Task.CompletedTask;
    }

    private void Invalidate(Guid agentId)
    {
        _cache.Remove(AgentListCacheKey);
        _cache.Remove(AgentCacheKey(agentId));
    }
}
