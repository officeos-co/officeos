using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents.Handlers;

//TODO also ich finde die loesung nicht wirklich clean. Kann der cache nicht einfach automatisch gecleant werden

internal sealed class InvalidateAgentCacheHandler :
    INotificationHandler<AgentCreatedEvent>,
    INotificationHandler<AgentDeletedEvent>,
    INotificationHandler<AgentUpdatedEvent>
{
    private const string AgentListCacheKey = "agents:list";
    private static string AgentCacheKey(Guid id) => $"agents:{id}";

    private readonly IDistributedCache _cache;

    public InvalidateAgentCacheHandler(IDistributedCache cache) => _cache = cache;

    public async Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        await InvalidateAsync(notification.AgentId, ct);
    }

    public async Task Handle(AgentDeletedEvent notification, CancellationToken ct)
    {
        await InvalidateAsync(notification.AgentId, ct);
    }

    public async Task Handle(AgentUpdatedEvent notification, CancellationToken ct)
    {
        await InvalidateAsync(notification.AgentId, ct);
    }

    private async Task InvalidateAsync(Guid agentId, CancellationToken ct)
    {
        await _cache.RemoveAsync(AgentListCacheKey, ct);
        await _cache.RemoveAsync(AgentCacheKey(agentId), ct);
    }
}
