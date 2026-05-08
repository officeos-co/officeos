namespace EnterpriseAgentOs.Api.Features.Analytics;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentLogsQueries
{
    [UsePaging(typeof(AgentLogDto), IncludeTotalCount = true, MaxPageSize = 500, DefaultPageSize = 100)]
    [GraphQLDescription("Returns log entries for a specific agent using HotChocolate cursor pagination.")]
    public IQueryable<AgentLogDto> GetAgentLogs(
        Guid agentId,
        [Service] IAgentLogService logs)
    {
        return logs.AgentLogs(agentId);
    }

    [UsePaging(typeof(AgentLogDto), IncludeTotalCount = true, MaxPageSize = 500, DefaultPageSize = 100)]
    [GraphQLDescription("Returns log entries for a specific channel connection using HotChocolate cursor pagination.")]
    public async Task<IQueryable<AgentLogDto>> GetChannelLogs(
        Guid channelConnectionId,
        [Service] UserContext user,
        [Service] IChannelRepository channels,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var connection = await channels.GetConnectionByAsync(new ChannelConnectionFilter
        {
            Id = channelConnectionId,
            CreatedById = user.Id,
        }, ct);
        if (connection is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Channel connection not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        return logs.ChannelLogs(channelConnectionId);
    }

    [UsePaging(typeof(AgentLogDto), IncludeTotalCount = true, MaxPageSize = 200, DefaultPageSize = 50)]
    [GraphQLDescription("Returns log entries across all agents using HotChocolate cursor pagination.")]
    public IQueryable<AgentLogDto> GetGlobalLogs(
        GlobalLogFiltersInput? filters,
        [Service] IAgentLogService logs)
    {
        return logs.GlobalLogs(filters ?? new GlobalLogFiltersInput());
    }

    [UseOffsetPaging(typeof(AuditEntry), IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 50)]
    [GraphQLDescription("Returns skill execution audit trail for an agent using HotChocolate offset pagination.")]
    public IQueryable<AuditEntry> GetAuditLog(
        Guid agentId,
        [Service] IAgentLogService logs)
    {
        return logs.AuditLog(agentId);
    }
}
