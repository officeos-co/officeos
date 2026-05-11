namespace OffceOs.Api.Features.AgentRoutines;

[ExtendObjectType(typeof(GraphQLMutations))]
public class RoutineMutations
{
    [GraphQLDescription("Creates an agent routine with schedule, API, and GitHub triggers.")]
    public async Task<CreateAgentRoutinePayload> CreateAgentRoutine(
        CreateAgentRoutineInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var result = await routines.CreateAsync(
            new CreateAgentRoutineRequest(
                input.AgentId,
                input.Name,
                input.Prompt,
                input.ScheduleTriggers?.Select(trigger => new CreateScheduleRoutineTriggerRequest(trigger.Name, trigger.Expression)).ToList() ?? [],
                input.ApiTriggers?.Select(trigger => new CreateApiRoutineTriggerRequest(trigger.Name)).ToList() ?? [],
                input.GitHubTriggers?.Select(trigger => new CreateGitHubRoutineTriggerRequest(trigger.Name, trigger.Owner, trigger.Repo, trigger.Events, trigger.Secret)).ToList() ?? []),
            user.Id,
            workspace.Id,
            ct);

        var row = await routines.GetForOwnerAsync(result.Routine.Id, user.Id, workspace.Id, ct);
        return AgentRoutineMapper.ToPayload(result, row?.AgentName ?? string.Empty);
    }

    [GraphQLDescription("Enables or disables an agent routine without deleting it.")]
    public async Task<bool> SetAgentRoutineEnabled(
        Guid id,
        bool enabled,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await routines.SetEnabledAsync(id, user.Id, workspace.Id, enabled, ct);
    }

    [GraphQLDescription("Permanently deletes an agent routine.")]
    public async Task<bool> DeleteAgentRoutine(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await routines.DeleteAsync(id, user.Id, workspace.Id, ct);
    }
}
