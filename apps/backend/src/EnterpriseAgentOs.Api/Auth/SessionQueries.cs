
namespace EnterpriseAgentOs.Api.Auth;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SessionQueries
{
    public async Task<IReadOnlyList<AgentSessionDto>> AgentSessions(
        Guid agentId,
        int? limit,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var records = await sessions.ListByAgentAsync(agentId, limit ?? 20, ct);
        return records.Select(AgentSessionDto.FromRecord).ToList();
    }

    public async Task<AgentSessionDto?> ActiveSession(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var active = await sessions.GetActiveAsync(agentId, ct);
        return active is not null ? AgentSessionDto.FromRecord(active) : null;
    }
}
