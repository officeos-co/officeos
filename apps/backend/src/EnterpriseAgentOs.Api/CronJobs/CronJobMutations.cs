
namespace EnterpriseAgentOs.Api.CronJobs;

[ExtendObjectType(typeof(GraphQLMutations))]
public class CronJobMutations
{
    public async Task<AgentCronJobRecord> CreateCronJob(
        CreateCronJobInput input,
        IResolverContext context,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await repo.CreateAsync(input.AgentId, input.Name, input.Expression, input.Prompt, ct);
    }

    public async Task<bool> SetCronJobEnabled(
        Guid id,
        bool enabled,
        IResolverContext context,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await repo.SetEnabledAsync(id, enabled, ct);
        return true;
    }

    public async Task<bool> DeleteCronJob(
        Guid id,
        IResolverContext context,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await repo.DeleteAsync(id, ct);
    }
}
