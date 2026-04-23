namespace EnterpriseAgentOs.Domain.Features.AgentPersonality;

public interface IAgentPersonalityRepository
{
    Task<IReadOnlyList<AgentPersonalityRecord>> ListAsync(Guid agentId, CancellationToken ct = default);
    Task UpsertAsync(Guid agentId, string fileName, string content, CancellationToken ct = default);
}
