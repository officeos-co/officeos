namespace EnterpriseAgentOs.Api.Features.Auth;

[ExtendObjectType(typeof(GraphQLMutations))]
public class SessionMutations
{
    public async Task<AgentSessionRecord> CreateSession(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentRepository agentRepo,
        [Service] IAgentSessionRepository sessions,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);

        var agent = await agentRepo.GetAsync(agentId, ct)
            ?? throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Agent not found.").SetCode("NOT_FOUND").Build());

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

        // Bootstrap from the agent's personality files (already on the aggregate)
        var bootstrapMsg = session.FormatBootstrapMessage(agent.PersonalityFiles, isFirstSession: isFirst);

        await logs.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            Time = DateTime.UtcNow,
            Type = AgentLogType.System,
            Content = bootstrapMsg,
        }, ct);

        return session;
    }

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
