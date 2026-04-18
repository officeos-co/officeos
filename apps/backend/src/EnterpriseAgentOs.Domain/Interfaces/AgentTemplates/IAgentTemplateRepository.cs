namespace EnterpriseAgentOs.Domain.Interfaces.AgentTemplates;

public interface IAgentTemplateRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord> UpsertAsync(EnterpriseAgentOs.Domain.Models.AgentTemplateRecord record, CancellationToken ct = default);
}
