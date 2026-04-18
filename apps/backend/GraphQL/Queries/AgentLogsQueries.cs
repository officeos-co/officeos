namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentLogsQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.DTOs.AgentLogs.AgentLogDto>> GetAgentLogs(
        Guid agentId,
        DateTime? before,
        int limit,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentLogs.IAgentLogService logs,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var capped = Math.Clamp(limit <= 0 ? 100 : limit, 1, 500);
        var rows = await logs.ListForAgentAsync(agentId, before, capped, ct);
        // descending from DB -> reverse to ascending for display
        return rows.OrderBy(r => r.Time).Select(r => EnterpriseAgentOs.Domain.DTOs.AgentLogs.AgentLogMapper.ToDto(r)).ToList();
    }

    public async Task<EnterpriseAgentOs.Domain.DTOs.AgentLogs.GlobalLogsPage> GetGlobalLogs(
        EnterpriseAgentOs.Domain.DTOs.AgentLogs.GlobalLogFiltersInput? filters,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentLogs.IAgentLogService logs,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await logs.ListGlobalAsync(filters ?? new EnterpriseAgentOs.Domain.DTOs.AgentLogs.GlobalLogFiltersInput(), ct);
    }

    public async Task<EnterpriseAgentOs.Domain.DTOs.AgentLogs.AuditLogPage> GetAuditLog(
        Guid agentId,
        int skip,
        int limit,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentLogs.IAgentLogService logs,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var capped = Math.Clamp(limit <= 0 ? 50 : limit, 1, 100);
        var offset = Math.Max(skip, 0);

        var (items, total) = await logs.GetAuditLogAsync(agentId, capped, offset, ct);

        var correlationIds = items
            .Where(r => r.CorrelationId is not null)
            .Select(r => r.CorrelationId!)
            .ToList();
        var results = await logs.GetResultsByCorrelationAsync(agentId, correlationIds, ct);

        var dtos = items.Select(r =>
        {
            string? resultSummary = null;
            long durationMs = r.DurationMs ?? 0;
            if (r.CorrelationId is not null && results.TryGetValue(r.CorrelationId, out var paired))
            {
                resultSummary = paired.Content;
                durationMs = paired.DurationMs ?? durationMs;
            }
            return new EnterpriseAgentOs.Domain.DTOs.AgentLogs.AuditEntry(
                r.Id,
                r.AgentId,
                null,
                r.Integration ?? string.Empty,
                r.Tool ?? string.Empty,
                r.Content,
                resultSummary,
                durationMs,
                r.Time);
        }).ToList();

        return new EnterpriseAgentOs.Domain.DTOs.AgentLogs.AuditLogPage(dtos, total);
    }
}
