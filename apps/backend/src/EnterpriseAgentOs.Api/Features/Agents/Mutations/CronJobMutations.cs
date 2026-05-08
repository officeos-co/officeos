
namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class CronJobMutations
{
    [GraphQLDescription("Creates a scheduled cron job for an agent. The job sends the specified prompt on the cron schedule.")]
    public async Task<AgentCronJobRecord> CreateCronJob(
        CreateCronJobInput input,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        return await repo.CreateAsync(input.AgentId, input.Name, input.Expression, input.Prompt, ct);
    }

    [GraphQLDescription("Enables or disables a cron job without deleting it.")]
    public async Task<bool> SetCronJobEnabled(
        Guid id,
        bool enabled,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        await repo.SetEnabledAsync(id, enabled, ct);
        return true;
    }

    [GraphQLDescription("Permanently deletes a cron job.")]
    public async Task<bool> DeleteCronJob(
        Guid id,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        return await repo.DeleteAsync(id, ct);
    }
}
