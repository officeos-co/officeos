using OffceOs.Domain.Features.Agents;
using OffceOs.Infrastructure.Common.Caching;
namespace OffceOs.Application.Features.Agents;

public static class AgentCacheKeys
{
    private const string ListIndexKey = "agents:list:index";
    private static readonly TimeSpan ListIndexTtl = TimeSpan.FromMinutes(10);

    public static string List(AgentFilter filter)
        => $"agents:list:id={filter.Id?.ToString() ?? "all"}:owner={filter.OwnerId?.ToString() ?? "all"}:workspace={filter.WorkspaceId?.ToString() ?? "all"}:deleted={filter.IncludeDeleted}";

    public static string Detail(AgentFilter filter)
        => $"agents:detail:id={filter.Id?.ToString() ?? "any"}:owner={filter.OwnerId?.ToString() ?? "any"}:workspace={filter.WorkspaceId?.ToString() ?? "any"}:deleted={filter.IncludeDeleted}";

    public static string ResourceList(Guid userId, Guid workspaceId) => $"agents:dashboard:list:{userId}:workspace:{workspaceId}";

    public static string ResourceDetail(Guid agentId, Guid userId, Guid workspaceId) => $"agents:dashboard:{agentId}:user:{userId}:workspace:{workspaceId}";

    public static async Task TrackListAsync(IDistributedCache cache, string cacheKey, CancellationToken ct)
    {
        var keys = await cache.GetJsonAsync<HashSet<string>>(ListIndexKey, ct) ?? [];
        if (keys.Add(cacheKey))
            await cache.SetJsonAsync(ListIndexKey, keys, ListIndexTtl, ct);
    }

    public static async Task InvalidateAgentAsync(IDistributedCache cache, Guid agentId, Guid? ownerId, CancellationToken ct)
    {
        await cache.RemoveAsync(Detail(new AgentFilter { Id = agentId }), ct);
        await cache.RemoveAsync(Detail(new AgentFilter { Id = agentId, IncludeDeleted = true }), ct);

        if (ownerId is not null)
        {
            await cache.RemoveAsync(Detail(new AgentFilter { Id = agentId, OwnerId = ownerId.Value }), ct);
            await cache.RemoveAsync(Detail(new AgentFilter { Id = agentId, OwnerId = ownerId.Value, IncludeDeleted = true }), ct);
        }

        await InvalidateTrackedListsAsync(cache, ct);
    }

    private static async Task InvalidateTrackedListsAsync(IDistributedCache cache, CancellationToken ct)
    {
        var keys = await cache.GetJsonAsync<HashSet<string>>(ListIndexKey, ct);
        if (keys is null) return;

        foreach (var key in keys)
            await cache.RemoveAsync(key, ct);

        await cache.RemoveAsync(ListIndexKey, ct);
    }
}
