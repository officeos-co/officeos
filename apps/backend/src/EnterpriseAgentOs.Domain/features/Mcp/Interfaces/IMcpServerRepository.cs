namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public interface IIntegrationDefinitionRepository
{
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(CancellationToken ct = default);
    Task<IntegrationDefinitionRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord> UpsertAsync(IntegrationDefinitionRecord server, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
}
