
namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class CronJobQueries
{
    [GraphQLDescription("Lists all scheduled cron jobs for agents owned by the authenticated user.")]
    public async Task<IReadOnlyList<CronJobPayload>> GetCronJobs(
        IResolverContext context,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListForOwnerAsync(user.Id, ct);
        return rows.Select(ToPayload).ToList();
    }

    [GraphQLDescription("Returns one scheduled cron job owned by the authenticated user.")]
    public async Task<CronJobPayload?> GetCronJob(
        Guid id,
        IResolverContext context,
        [Service] IAgentCronJobRepository repo,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await repo.GetForOwnerAsync(id, user.Id, ct);
        return row is null ? null : ToPayload(row);
    }

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

    private static CronJobPayload ToPayload(AgentCronJobWithAgentRecord row) =>
        new(
            row.Job.Id,
            row.Job.AgentId,
            row.AgentName,
            row.Job.Name,
            row.Job.Expression,
            row.Job.Prompt,
            row.Job.Enabled,
            row.Job.LastRunAt,
            row.Job.NextRunAt,
            row.Job.CreatedAt);
}
