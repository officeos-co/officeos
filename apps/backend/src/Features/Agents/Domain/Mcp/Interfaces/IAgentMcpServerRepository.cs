namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public interface IAgentIntegrationRepository
{
    Task<IReadOnlyList<string>> ListIntegrationNamesForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignAsync(Guid agentId, string integrationName, CancellationToken ct = default);
    Task UnassignAsync(Guid agentId, string integrationName, CancellationToken ct = default);
    Task UnassignIntegrationFromOwnerAgentsAsync(Guid ownerId, string integrationName, CancellationToken ct = default);
}
