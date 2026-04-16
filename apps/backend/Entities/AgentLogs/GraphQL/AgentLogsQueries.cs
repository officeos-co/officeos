using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.AgentLogs.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentLogsQueries
{
    public async Task<IReadOnlyList<AgentLogDto>> GetAgentLogs(
        Guid agentId,
        DateTime? before,
        int limit,
        IResolverContext context,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var capped = Math.Clamp(limit <= 0 ? 100 : limit, 1, 500);
        var rows = await logs.ListForAgentAsync(agentId, before, capped, ct);
        // descending from DB -> reverse to ascending for display
        return rows.OrderBy(r => r.Time).Select(r => r.ToDto()).ToList();
    }

    public async Task<GlobalLogsPage> GetGlobalLogs(
        GlobalLogFiltersInput? filters,
        IResolverContext context,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await logs.ListGlobalAsync(filters ?? new GlobalLogFiltersInput(), ct);
    }
}
