namespace OffceOs.Api.Features.Integrations;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class IntegrationDefinitionQueries
{
    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetIntegrations(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await svc.ListAsync(user.Id, workspace.Id, ct);
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetIntegrationCatalog(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await svc.ListCatalogAsync(user.Id, workspace.Id, ct);
    }

    public async Task<IntegrationDefinitionRecord?> GetIntegration(
        string name,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await svc.GetAsync(user.Id, name, workspace.Id, ct);
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> GetAgentIntegrations(
        Guid agentId,
        [Service] UserContext user,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
        => await svc.ListForAgentAsync(agentId, user.Id, ct);
}
