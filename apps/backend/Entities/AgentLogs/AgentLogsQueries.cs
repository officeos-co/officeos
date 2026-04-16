namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentLogsQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogDto>> GetAgentLogs(
        Guid agentId,
        DateTime? before,
        int limit,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogService logs,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var capped = Math.Clamp(limit <= 0 ? 100 : limit, 1, 500);
        var rows = await logs.ListForAgentAsync(agentId, before, capped, ct);
        // descending from DB -> reverse to ascending for display
        return rows.OrderBy(r => r.Time).Select(r => EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogMapper.ToDto(r)).ToList();
    }

    public async Task<EnterpriseAgentOs.Api.Entities.AgentLogs.GlobalLogsPage> GetGlobalLogs(
        EnterpriseAgentOs.Api.Entities.AgentLogs.GlobalLogFiltersInput? filters,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogService logs,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await logs.ListGlobalAsync(filters ?? new EnterpriseAgentOs.Api.Entities.AgentLogs.GlobalLogFiltersInput(), ct);
    }
}
