namespace EnterpriseAgentOs.Api.Features.Analytics;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentLogsQueries
{
    [UsePaging(typeof(AgentLogDto), IncludeTotalCount = true, MaxPageSize = 500, DefaultPageSize = 100)]
    [GraphQLDescription("Returns log entries for a specific agent using HotChocolate cursor pagination.")]
    public IQueryable<AgentLogDto> GetAgentLogs(
        Guid agentId,
        IResolverContext context,
        [Service] EaosDbContext db)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return ProjectAgentLogs(
            db.AgentLogs
                .AsNoTracking()
                .Where(l => l.AgentId == agentId)
                .OrderBy(l => l.Time)
                .ThenBy(l => l.Id));
    }

    [UsePaging(typeof(AgentLogDto), IncludeTotalCount = true, MaxPageSize = 500, DefaultPageSize = 100)]
    [GraphQLDescription("Returns log entries for a specific channel connection using HotChocolate cursor pagination.")]
    public async Task<IQueryable<AgentLogDto>> GetChannelLogs(
        Guid channelConnectionId,
        IResolverContext context,
        [Service] IChannelRepository channels,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
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

        return ProjectAgentLogs(
            db.AgentLogs
                .AsNoTracking()
                .Where(l => l.ChannelConnectionId == channelConnectionId)
                .OrderBy(l => l.Time)
                .ThenBy(l => l.Id));
    }

    [UsePaging(typeof(AgentLogDto), IncludeTotalCount = true, MaxPageSize = 200, DefaultPageSize = 50)]
    [GraphQLDescription("Returns log entries across all agents using HotChocolate cursor pagination.")]
    public IQueryable<AgentLogDto> GetGlobalLogs(
        GlobalLogFiltersInput? filters,
        IResolverContext context,
        [Service] EaosDbContext db)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        filters ??= new GlobalLogFiltersInput();

        var query = db.AgentLogs
            .AsNoTracking()
            .Include(l => l.Agent)
            .AsQueryable();

        if (filters.Type.HasValue)
        {
            var type = filters.Type.Value;
            query = query.Where(l => l.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(filters.AgentName))
        {
            var needle = filters.AgentName.Trim();
            query = query.Where(l => l.Agent != null && EF.Functions.ILike(l.Agent.Name, $"%{needle}%"));
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var needle = filters.Search.Trim();
            query = query.Where(l => EF.Functions.ILike(l.Content, $"%{needle}%"));
        }

        return ProjectAgentLogs(
            query
                .OrderByDescending(l => l.Time)
                .ThenByDescending(l => l.Id));
    }

    [UseOffsetPaging(typeof(AuditEntry), IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 50)]
    [GraphQLDescription("Returns skill execution audit trail for an agent using HotChocolate offset pagination.")]
    public IQueryable<AuditEntry> GetAuditLog(
        Guid agentId,
        IResolverContext context,
        [Service] EaosDbContext db)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var results = db.AgentLogs
            .AsNoTracking()
            .Where(r => r.AgentId == agentId
                        && r.Type == AgentLogType.ToolResult
                        && r.CorrelationId != null);

        return
            from call in db.AgentLogs.AsNoTracking()
            where call.AgentId == agentId && call.Type == AgentLogType.ToolCall
            join result in results on call.CorrelationId equals result.CorrelationId into pairedResults
            from result in pairedResults.DefaultIfEmpty()
            orderby call.Time descending, call.Id descending
            select new AuditEntry(
                call.Id,
                call.AgentId,
                null,
                call.Integration ?? string.Empty,
                call.Tool ?? string.Empty,
                call.Content,
                result == null ? null : result.Content,
                result == null
                    ? call.DurationMs ?? 0
                    : result.DurationMs ?? call.DurationMs ?? 0,
                call.Time);
    }

    private static IQueryable<AgentLogDto> ProjectAgentLogs(IQueryable<AgentLogEntity> query) =>
        query.Select(log => new AgentLogDto(
            log.Id,
            log.AgentId,
            log.Agent == null ? null : log.Agent.Name,
            log.Time,
            log.Type,
            log.Tool,
            log.Integration,
            log.Channel,
            log.ChannelConnectionId,
            log.Content,
            log.DurationMs,
            log.InputTokens,
            log.OutputTokens,
            log.CorrelationId));
}
