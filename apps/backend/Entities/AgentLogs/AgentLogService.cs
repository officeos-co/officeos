namespace EnterpriseAgentOs.Api.Entities.AgentLogs;

public sealed class AgentLogService : IAgentLogService
{
    private readonly IAgentLogRepository _repo;
    private readonly ITopicEventSender _sender;
    private readonly IAgentRepository _agents;

    public AgentLogService(IAgentLogRepository repo, ITopicEventSender sender, IAgentRepository agents)
    {
        _repo = repo;
        _sender = sender;
        _agents = agents;
    }

    public Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default)
        => _repo.ListAsync(agentId, before, limit, ct);

    public async Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersInput filters, CancellationToken ct = default)
    {
        var limit = Math.Clamp(filters.Limit, 1, 200);
        var skip = Math.Max(filters.Skip, 0);
        var (rows, total) = await _repo.ListGlobalAsync(filters.Search, filters.AgentName, filters.Type, skip, limit, ct);
        var items = rows.Select(r => r.Log.ToDto(r.AgentName)).ToList();
        return new GlobalLogsPage(items, total);
    }

    public async Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        var saved = await _repo.AppendAsync(record, ct);
        await _sender.SendAsync($"agent-log:{saved.AgentId}", saved.ToDto(), ct);
        return saved;
    }

    public async Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default)
    {
        var agent = await _agents.GetAsync(agentId, ct)
            ?? throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Agent '{agentId}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());

        var record = new AgentLogRecord
        {
            AgentId = agent.Id,
            Time = DateTime.UtcNow,
            Type = AgentLogType.MessageIn,
            Content = content,
            CorrelationId = Guid.NewGuid().ToString(),
        };

        // TODO: kick the agent pod
        return await AppendAsync(record, ct);
    }
}
