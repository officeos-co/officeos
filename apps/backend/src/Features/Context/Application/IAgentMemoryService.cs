using OffceOs.Domain.Features.Context;

namespace OffceOs.Application.Features.Context;

public interface IAgentMemoryService
{
    Task StoreAsync(Guid agentId, string key, string content, CancellationToken ct = default);
    Task<IReadOnlyList<AgentMemoryRecord>> RecallAsync(Guid agentId, string query, int limit, CancellationToken ct = default);
    Task<bool> ForgetAsync(Guid agentId, string key, CancellationToken ct = default);
}
