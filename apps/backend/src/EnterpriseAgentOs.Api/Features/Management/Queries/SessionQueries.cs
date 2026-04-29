
namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SessionQueries
{
    [GraphQLDescription("Lists conversation sessions for an agent, ordered by most recent. Default limit is 20.")]
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

    [GraphQLDescription("Returns the currently active session for an agent, or null if no session is active.")]
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
