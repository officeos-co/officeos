namespace OffceOs.Api.Features.Integrations;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class IntegrationDefinitionQueries
{
    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetIntegrations(
        [Service] UserContext user,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.ListAsync(user.Id, ct);

    public async Task<IntegrationDefinitionRecord?> GetIntegration(
        string name,
        [Service] UserContext user,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.GetAsync(user.Id, name, ct);

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetAgentIntegrations(
        Guid agentId,
        [Service] UserContext user,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.ListForAgentAsync(agentId, user.Id, ct);
}
