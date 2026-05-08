namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public interface IAgentIntegrationDefinitionRepository
{
    Task<IReadOnlyList<string>> ListIntegrationNamesForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignAsync(Guid agentId, string mcpIntegrationName, CancellationToken ct = default);
    Task UnassignAsync(Guid agentId, string mcpIntegrationName, CancellationToken ct = default);
    Task UnassignServerFromAllAgentsAsync(string mcpIntegrationName, CancellationToken ct = default);
}
