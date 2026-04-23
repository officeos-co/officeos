namespace EnterpriseAgentOs.Api.Features.Auth;

[ExtendObjectType(typeof(GraphQLMutations))]
public class SessionMutations
{
    /// <summary>
    /// Creates a new session for an agent, ending any active session.
    /// Sends a bootstrap message to initialize the new session context.
    /// </summary>
    public async Task<AgentSessionRecord> CreateSession(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        [Service] IAgentPersonalityRepository personalities,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);

        // End any active session
        var active = await sessions.GetActiveAsync(agentId, ct);
        if (active is not null)
        {
            active.End();
            await sessions.SaveChangesAsync(ct);
        }

        // Create new session
        var isFirst = await sessions.CountByAgentAsync(agentId, ct) == (active is not null ? 1 : 0);
        var session = AgentSessionRecord.Create(agentId);
        await sessions.CreateAsync(session, ct);

        // Send bootstrap message
        var personalityFiles = await personalities.ListAsync(agentId, ct);
        var bootstrapMsg = session.FormatBootstrapMessage(personalityFiles, isFirstSession: isFirst);

        var log = new AgentLogRecord
        {
            AgentId = agentId,
            Time = DateTime.UtcNow,
            Type = AgentLogType.System,
            Content = bootstrapMsg,
        };
        await logs.AppendAsync(log, ct);

        return session;
    }

    /// <summary>Ends the active session for an agent.</summary>
    public async Task<AgentSessionRecord?> EndSession(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var active = await sessions.GetActiveAsync(agentId, ct);
        if (active is null) return null;

        active.End();
        await sessions.SaveChangesAsync(ct);
        return active;
    }
}
