
namespace EnterpriseAgentOs.Api.Features.CronJobs;

[ExtendObjectType(typeof(GraphQLQueries))]
public class CronJobQueries
{
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
