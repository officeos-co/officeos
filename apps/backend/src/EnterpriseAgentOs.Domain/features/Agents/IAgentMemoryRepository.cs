namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IAgentMemoryRepository
{
    Task<AgentMemoryRecord?> GetAsync(Guid agentId, string key, CancellationToken ct = default);
    Task<IReadOnlyList<AgentMemoryRecord>> ListAsync(Guid agentId, CancellationToken ct = default);
    Task UpsertAsync(Guid agentId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid agentId, string key, CancellationToken ct = default);
}
