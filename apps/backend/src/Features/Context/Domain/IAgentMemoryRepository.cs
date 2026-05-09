namespace EnterpriseAgentOs.Domain.Features.Context;

public interface IAgentMemoryRepository
{
    Task<AgentMemoryRecord?> GetByAsync(AgentMemoryFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AgentMemoryRecord>> ListAsync(Guid agentId, CancellationToken ct = default);
    Task UpsertAsync(Guid agentId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid agentId, string key, CancellationToken ct = default);
}
