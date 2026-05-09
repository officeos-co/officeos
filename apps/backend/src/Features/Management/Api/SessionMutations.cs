namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public class SessionMutations
{
    [GraphQLDescription("Creates a new conversation session for an agent. Ends any active session first and appends a bootstrap system message with personality files.")]
    public async Task<AgentSessionRecord> CreateSession(
        Guid agentId,
        [Service] UserContext user,
        [Service] IAgentSessionService sessions,
        CancellationToken ct)
    {
        try
        {
            return await sessions.CreateAsync(agentId, user.Id, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("NOT_FOUND").Build());
        }
    }

    [GraphQLDescription("Ends the active session for an agent. Returns the ended session or null if none was active.")]
    public async Task<AgentSessionRecord?> EndSession(
        Guid agentId,
        [Service] UserContext user,
        [Service] IAgentSessionService sessions,
        CancellationToken ct)
    {
        return await sessions.EndAsync(agentId, user.Id, ct);
    }
}
