
namespace EnterpriseAgentOs.Api.Auth;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SessionQueries
{
    public async Task<IReadOnlyList<AgentSessionRecord>> AgentSessions(
        Guid agentId,
        int? limit,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await sessions.ListByAgentAsync(agentId, limit ?? 20, ct);
    }

    public async Task<AgentSessionRecord?> ActiveSession(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await sessions.GetActiveAsync(agentId, ct);
    }
}
