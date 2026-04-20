namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class SessionMutations
{
    /// <summary>
    /// Creates a new session for an agent, ending any active session.
    /// Sends a bootstrap message to initialize the new session context.
    /// </summary>
    public async Task<AgentSessionDto> CreateSession(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        [Service] IAgentPersonalityRepository personalities,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);

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

        return AgentSessionDto.FromRecord(session);
    }

    /// <summary>Ends the active session for an agent.</summary>
    public async Task<AgentSessionDto?> EndSession(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSessionRepository sessions,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);

        var active = await sessions.GetActiveAsync(agentId, ct);
        if (active is null) return null;

        active.End();
        await sessions.SaveChangesAsync(ct);
        return AgentSessionDto.FromRecord(active);
    }
}

/// <summary>GraphQL DTO for agent sessions.</summary>
public sealed record AgentSessionDto(
    Guid Id,
    Guid AgentId,
    string Status,
    int MessageCount,
    DateTime LastActivityAt,
    DateTime CreatedAt,
    DateTime? EndedAt)
{
    public static AgentSessionDto FromRecord(AgentSessionRecord r) =>
        new(r.Id, r.AgentId, r.Status, r.MessageCount, r.LastActivityAt, r.CreatedAt, r.EndedAt);
}
