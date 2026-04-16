namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AuditQueries
{
    public async Task<EnterpriseAgentOs.Api.Entities.Audit.Types.AuditLogPage> GetAuditLog(
        Guid agentId,
        int skip,
        int limit,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Audit.IAuditService audit,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var capped = Math.Clamp(limit <= 0 ? 50 : limit, 1, 100);
        var offset = Math.Max(skip, 0);

        var (items, total) = await audit.GetAuditLogAsync(agentId, capped, offset);

        var dtos = items.Select(r => new EnterpriseAgentOs.Api.Entities.Audit.Types.AuditEntry(
            r.Id,
            r.AgentId,
            null,
            r.Integration ?? string.Empty,
            r.Tool ?? string.Empty,
            r.Content,
            null,
            r.DurationMs ?? 0,
            r.Time)).ToList();

        return new EnterpriseAgentOs.Api.Entities.Audit.Types.AuditLogPage(dtos, total);
    }
}
