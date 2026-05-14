namespace OffceOs.Api.Features.Analytics;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentLogsQueries
{
    [UsePaging(typeof(AgentLogProjection), IncludeTotalCount = true, MaxPageSize = 500, DefaultPageSize = 100)]
    [GraphQLDescription("Returns log entries for a specific agent using HotChocolate cursor pagination.")]
    public async Task<IQueryable<AgentLogProjection>> GetAgentLogs(
        Guid agentId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRepository agents,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var agent = await agents.GetByAsync(new AgentFilter { Id = agentId, WorkspaceId = workspace.Id }, ct);
        if (agent is null)
            throw new GraphQLException(ErrorBuilder.New().SetMessage("Agent not found.").SetCode("NOT_FOUND").Build());

        return logs.AgentLogs(agentId, workspace.Id);
    }

    [UsePaging(typeof(AgentLogProjection), IncludeTotalCount = true, MaxPageSize = 500, DefaultPageSize = 100)]
    [GraphQLDescription("Returns log entries for a specific channel connection using HotChocolate cursor pagination.")]
    public async Task<IQueryable<AgentLogProjection>> GetChannelLogs(
        Guid channelConnectionId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelRepository channels,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var connection = await channels.GetConnectionByAsync(new ChannelConnectionFilter
        {
            Id = channelConnectionId,
            CreatedById = user.Id,
            WorkspaceId = workspace.Id,
        }, ct);
        if (connection is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Channel connection not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        return logs.ChannelLogs(channelConnectionId, workspace.Id);
    }

    [GraphQLDescription("Returns log entries across all agents using offset pagination.")]
    public async Task<GlobalLogsPage> GetGlobalLogs(
        GlobalLogFiltersInput? filters,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var request = filters is null
            ? new GlobalLogFiltersRequest(WorkspaceId: workspace.Id)
            : new GlobalLogFiltersRequest(filters.Search, filters.AgentName, filters.Type, filters.Skip, filters.Limit, workspace.Id);

        return await logs.ListGlobalAsync(request);
    }

    [UseOffsetPaging(typeof(AuditEntry), IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 50)]
    [GraphQLDescription("Returns skill execution audit trail for an agent using HotChocolate offset pagination.")]
    public async Task<IQueryable<AuditEntry>> GetAuditLog(
        Guid agentId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRepository agents,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var agent = await agents.GetByAsync(new AgentFilter { Id = agentId, WorkspaceId = workspace.Id }, ct);
        if (agent is null)
            throw new GraphQLException(ErrorBuilder.New().SetMessage("Agent not found.").SetCode("NOT_FOUND").Build());

        return logs.AuditLog(agentId, workspace.Id);
    }
}
