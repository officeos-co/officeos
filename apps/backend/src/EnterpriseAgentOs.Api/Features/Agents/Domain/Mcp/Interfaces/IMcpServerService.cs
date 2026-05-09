namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public interface IIntegrationDefinitionService
{
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord?> GetAsync(Guid ownerId, string name, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord> RegisterAsync(Guid ownerId, IntegrationDefinitionRecord server, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerId, string name, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListForAgentAsync(Guid agentId, Guid? ownerId = null, CancellationToken ct = default);
    Task AssignToAgentAsync(Guid agentId, string integrationName, Guid? ownerId = null, CancellationToken ct = default);
    Task UnassignFromAgentAsync(Guid agentId, string integrationName, CancellationToken ct = default);
    Task SaveCredentialAsync(Guid ownerId, string integrationName, Dictionary<string, string> fields, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetDecryptedCredentialAsync(string integrationName, Guid? ownerId = null, CancellationToken ct = default);
}
