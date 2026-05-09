
namespace OffceOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class CronJobMutations
{
    [GraphQLDescription("Creates a scheduled cron job for an agent. The job sends the specified prompt on the cron schedule.")]
    public async Task<AgentCronJobRecord> CreateCronJob(
        CreateCronJobInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentCronJobService jobs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await jobs.CreateAsync(
            new CreateAgentCronJobRequest(input.AgentId, input.Name, input.Expression, input.Prompt),
            user.Id,
            workspace.Id,
            ct);
    }

    [GraphQLDescription("Enables or disables a cron job without deleting it.")]
    public async Task<bool> SetCronJobEnabled(
        Guid id,
        bool enabled,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentCronJobService jobs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await jobs.SetEnabledAsync(id, user.Id, workspace.Id, enabled, ct);
    }

    [GraphQLDescription("Permanently deletes a cron job.")]
    public async Task<bool> DeleteCronJob(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentCronJobService jobs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await jobs.DeleteAsync(id, user.Id, workspace.Id, ct);
    }
}
