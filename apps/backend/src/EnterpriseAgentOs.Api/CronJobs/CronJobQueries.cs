
namespace EnterpriseAgentOs.Api.CronJobs;

[ExtendObjectType(typeof(GraphQLQueries))]
public class CronJobQueries
{
    public async Task<IReadOnlyList<CronJobPayload>> GetAgentCronJobs(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var records = await repo.ListAsync(agentId, ct);
        return records.Select(r => new CronJobPayload(
            r.Id, r.AgentId, r.Name, r.Expression, r.Prompt,
            r.Enabled, r.LastRunAt, r.NextRunAt, r.CreatedAt)).ToList();
    }
}
