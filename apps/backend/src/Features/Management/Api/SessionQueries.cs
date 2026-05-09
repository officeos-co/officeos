
namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SessionQueries
{
    [GraphQLDescription("Lists conversation sessions for an agent, ordered by most recent. Default limit is 20.")]
    public async Task<IReadOnlyList<AgentSessionRecord>> AgentSessions(
        Guid agentId,
        int? limit,
        [Service] UserContext user,
        [Service] IAgentSessionService sessions,
        CancellationToken ct)
    {
        return await sessions.ListByAgentAsync(agentId, user.Id, limit ?? 20, ct);
    }

    [GraphQLDescription("Returns the currently active session for an agent, or null if no session is active.")]
    public async Task<AgentSessionRecord?> ActiveSession(
        Guid agentId,
        [Service] UserContext user,
        [Service] IAgentSessionService sessions,
        CancellationToken ct)
    {
        return await sessions.GetActiveAsync(agentId, user.Id, ct);
    }
}
