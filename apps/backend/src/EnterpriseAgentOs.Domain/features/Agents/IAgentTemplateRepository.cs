namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IAgentTemplateRepository
{
    Task<IReadOnlyList<AgentTemplateRecord>> ListAsync(CancellationToken ct = default);
    Task<AgentTemplateRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AgentTemplateRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<AgentTemplateRecord> UpsertAsync(AgentTemplateRecord record, CancellationToken ct = default);
}
