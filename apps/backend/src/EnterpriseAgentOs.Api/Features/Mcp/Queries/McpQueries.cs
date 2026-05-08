using EnterpriseAgentOs.Api.Common;
using EnterpriseAgentOs.Domain.Features.Agents.Integrations;

namespace EnterpriseAgentOs.Api.Features.Agents.Integrations;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class McpQueries
{
    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetMcpServers(
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.ListAsync(ct);

    public async Task<IntegrationDefinitionRecord?> GetMcpServer(
        string name,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.GetAsync(name, ct);

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetAgentMcpServers(
        Guid agentId,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.ListForAgentAsync(agentId, ct);
}
