
namespace EnterpriseAgentOs.Api.Features.CronJobs;

[ExtendObjectType(typeof(GraphQLQueries))]
public class CronJobQueries
{
    [GraphQLDescription("Lists all scheduled cron jobs for a specific agent.")]
    public async Task<IReadOnlyList<AgentCronJobRecord>> GetAgentCronJobs(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await repo.ListAsync(agentId, ct);
    }
}
