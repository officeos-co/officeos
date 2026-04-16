namespace EnterpriseAgentOs.Api.Entities.AgentTemplates;

public interface IAgentTemplateRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.AgentTemplateRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentTemplateRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentTemplateRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentTemplateRecord> UpsertAsync(EnterpriseAgentOs.Api.Database.Models.AgentTemplateRecord record, CancellationToken ct = default);
}
