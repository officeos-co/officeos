namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public interface IIntegrationDefinitionService
{
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(CancellationToken ct = default);
    Task<IntegrationDefinitionRecord?> GetAsync(string name, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord> RegisterAsync(IntegrationDefinitionRecord server, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignToAgentAsync(Guid agentId, string integrationName, CancellationToken ct = default);
    Task UnassignFromAgentAsync(Guid agentId, string integrationName, CancellationToken ct = default);
    Task SaveCredentialAsync(string integrationName, Dictionary<string, string> fields, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetDecryptedCredentialAsync(string integrationName, CancellationToken ct = default);
}
