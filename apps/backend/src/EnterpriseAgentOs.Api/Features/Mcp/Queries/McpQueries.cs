using EnterpriseAgentOs.Api.Common;
using EnterpriseAgentOs.Domain.Features.Agents.Integrations;

namespace EnterpriseAgentOs.Api.Features.Agents.Integrations;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class IntegrationDefinitionQueries
{
    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetIntegrations(
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.ListAsync(ct);

    public async Task<IntegrationDefinitionRecord?> GetIntegration(
        string name,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.GetAsync(name, ct);

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetAgentIntegrations(
        Guid agentId,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.ListForAgentAsync(agentId, ct);
}
