using OffceOs.Features.Agents.Domain;

namespace OffceOs.Features.Agents.Application;

public interface IAgentSessionService
{
    Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, Guid ownerId, int limit = 20, CancellationToken ct = default);
    Task<AgentSessionRecord?> GetActiveAsync(Guid agentId, Guid ownerId, CancellationToken ct = default);
    Task<AgentSessionRecord> CreateAsync(Guid agentId, Guid ownerId, CancellationToken ct = default);
    Task<AgentSessionRecord?> EndAsync(Guid agentId, Guid ownerId, CancellationToken ct = default);
}
