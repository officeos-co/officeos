namespace OffceOs.Domain.Features.Agents;

public interface IAgentDefinitionRepository
{
    Task<AgentDefinitionRecord?> GetByAsync(AgentDefinitionFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AgentDefinitionRecord>> ListAsync(AgentDefinitionFilter filter, CancellationToken ct = default);
    Task AddAsync(AgentDefinitionRecord definition, CancellationToken ct = default);
    Task<int> GetNextVersionAsync(Guid agentId, CancellationToken ct = default);
}
