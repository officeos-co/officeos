namespace OffceOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentQueries
{
    [GraphQLDescription("Lists all agents owned by the authenticated user with dynamic status and last relevant activity.")]
    public async Task<IReadOnlyList<AgentPayload>> GetAgents(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var result = await agents.ListDashboardAgentsAsync(user.Id, workspace.Id, ct);
        return result
            .Select(row => AgentGraphQLMapper.ToPayload(row.Agent, row.Status, row.LastRelevantMessage))
            .ToList();
    }

    [GraphQLDescription("Returns a single agent by ID including its full aggregate, dynamic status, and last relevant activity.")]
    public async Task<AgentPayload?> GetAgent(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var result = await agents.GetDashboardAgentAsync(id, user.Id, workspace.Id, ct);
        return result is null
            ? null
            : AgentGraphQLMapper.ToPayload(result.Agent, result.Status, result.LastRelevantMessage);
    }

    [GraphQLDescription("Returns the backend-owned tool catalog for dashboard permission UIs.")]
    public async Task<IReadOnlyList<AgentToolCatalogEntry>> GetAgentToolCatalog(
        Guid? agentId,
        [Service] IAgentToolCatalogService catalog,
        CancellationToken ct)
    {
        return await catalog.ListAsync(agentId, ct);
    }

    [GraphQLDescription("Lists persisted runs for an agent, optionally filtered by parent run.")]
    public async Task<IReadOnlyList<AgentRunRecord>> GetAgentRuns(
        Guid agentId,
        Guid? parentRunId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await agents.ListRunsAsync(user.Id, workspace.Id, agentId, parentRunId, ct);
    }
}
