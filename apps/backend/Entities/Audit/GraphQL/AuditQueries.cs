using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.Audit.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AuditQueries
{
    public async Task<AuditLogPage> GetAuditLog(
        Guid agentId,
        int skip,
        int limit,
        IResolverContext context,
        [Service] IAuditService audit,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var capped = Math.Clamp(limit <= 0 ? 50 : limit, 1, 100);
        var offset = Math.Max(skip, 0);

        var (items, total) = await audit.GetAuditLogAsync(agentId, capped, offset);

        var dtos = items.Select(r => new AuditEntry(
            r.Id,
            r.AgentId,
            null,
            r.Integration ?? string.Empty,
            r.Tool ?? string.Empty,
            r.Content,
            null,
            r.DurationMs ?? 0,
            r.Time)).ToList();

        return new AuditLogPage(dtos, total);
    }
}
