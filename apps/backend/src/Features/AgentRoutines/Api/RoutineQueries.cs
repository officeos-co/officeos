namespace OffceOs.Api.Features.AgentRoutines;

[ExtendObjectType(typeof(GraphQLQueries))]
public class RoutineQueries
{
    [GraphQLDescription("Lists all routines for agents in the authenticated user's current workspace.")]
    public async Task<IReadOnlyList<AgentRoutinePayload>> GetAgentRoutines(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var rows = await routines.ListForOwnerAsync(user.Id, workspace.Id, ct);
        return rows.Select(AgentRoutineMapper.ToPayload).ToList();
    }

    [GraphQLDescription("Returns one routine in the authenticated user's current workspace.")]
    public async Task<AgentRoutinePayload?> GetAgentRoutine(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var row = await routines.GetForOwnerAsync(id, user.Id, workspace.Id, ct);
        return row is null ? null : AgentRoutineMapper.ToPayload(row);
    }

    [GraphQLDescription("Lists all routines for a specific agent in the authenticated user's current workspace.")]
    public async Task<IReadOnlyList<AgentRoutineRecord>> GetRoutinesForAgent(
        Guid agentId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await routines.ListForAgentAsync(agentId, user.Id, workspace.Id, ct);
    }
}
