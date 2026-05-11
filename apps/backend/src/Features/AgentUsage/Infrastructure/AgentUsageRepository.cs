namespace OffceOs.Infrastructure.Features.AgentUsage;

internal sealed class AgentUsageRepository : IAgentUsageRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentUsageRepository(EaosDbContext eaosDbContext)
    {
        _eaosDbContext = eaosDbContext;
    }

    public IQueryable<AgentUsageCallRecord> Query(AgentUsageFilter filter)
    {
        var query = ApplyFilter(_eaosDbContext.Set<AgentUsageCallEntity>().AsNoTracking(), filter);
        return Project(query);
    }

    public async Task<List<AgentUsageCallRecord>> ListAsync(AgentUsageFilter filter, CancellationToken ct = default)
    {
        return await Query(filter)
            .OrderBy(c => c.Time)
            .ThenBy(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task<AgentUsageCallRecord> SaveAsync(AgentUsageCallRecord record, CancellationToken ct = default)
    {
        var agentScope = await _eaosDbContext.Agents
            .AsNoTracking()
            .Where(a => a.Id == record.AgentId)
            .Select(a => new { a.OwnerId, a.WorkspaceId })
            .FirstOrDefaultAsync(ct);

        var callId = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        var entity = new AgentUsageCallEntity
        {
            Id = callId,
            AgentId = record.AgentId,
            WorkspaceId = record.WorkspaceId ?? agentScope?.WorkspaceId,
            OwnerId = record.OwnerId ?? agentScope?.OwnerId,
            RunId = record.RunId,
            ParentRunId = record.ParentRunId,
            CorrelationId = record.CorrelationId,
            Time = record.Time,
            Provider = record.Provider,
            Model = record.Model,
            DurationMs = record.DurationMs,
            InputTokens = record.InputTokens,
            OutputTokens = record.OutputTokens,
            CacheReadTokens = record.CacheReadTokens,
            CacheWriteTokens = record.CacheWriteTokens,
            ReasoningTokens = record.ReasoningTokens,
            EstimatedTokens = record.EstimatedTokens,
            Credits = record.Credits,
            Activity = record.Activity,
            Outcome = record.Outcome,
            ContextParts = record.ContextParts.Select(part => new AgentUsageContextPartEntity
            {
                Id = part.Id == Guid.Empty ? Guid.NewGuid() : part.Id,
                CallId = callId,
                Kind = part.Kind,
                Label = part.Label,
                Role = part.Role,
                Tool = part.Tool,
                Integration = part.Integration,
                Tokens = part.Tokens,
                EstimatedTokens = part.EstimatedTokens,
                CharacterCount = part.CharacterCount,
            }).ToList(),
        };

        _eaosDbContext.Set<AgentUsageCallEntity>().Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);

        return ToRecord(entity);
    }

    private static IQueryable<AgentUsageCallEntity> ApplyFilter(
        IQueryable<AgentUsageCallEntity> query,
        AgentUsageFilter filter)
    {
        if (filter.OwnerId.HasValue)
            query = query.Where(c => c.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(c => c.WorkspaceId == filter.WorkspaceId.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(c => c.AgentId == filter.AgentId.Value);

        if (filter.RunId.HasValue)
            query = query.Where(c => c.RunId == filter.RunId.Value);

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
            query = query.Where(c => c.CorrelationId == filter.CorrelationId);

        if (!string.IsNullOrWhiteSpace(filter.Provider))
            query = query.Where(c => c.Provider == filter.Provider);

        if (!string.IsNullOrWhiteSpace(filter.Model))
            query = query.Where(c => c.Model == filter.Model);

        if (filter.FromInclusive.HasValue)
            query = query.Where(c => c.Time >= filter.FromInclusive.Value);

        if (filter.ToExclusive.HasValue)
            query = query.Where(c => c.Time < filter.ToExclusive.Value);

        return query;
    }

    private static IQueryable<AgentUsageCallRecord> Project(IQueryable<AgentUsageCallEntity> query) =>
        query.Select(call => new AgentUsageCallRecord
        {
            Id = call.Id,
            AgentId = call.AgentId,
            WorkspaceId = call.WorkspaceId,
            OwnerId = call.OwnerId,
            RunId = call.RunId,
            ParentRunId = call.ParentRunId,
            CorrelationId = call.CorrelationId,
            Time = call.Time,
            Provider = call.Provider,
            Model = call.Model,
            DurationMs = call.DurationMs,
            InputTokens = call.InputTokens,
            OutputTokens = call.OutputTokens,
            CacheReadTokens = call.CacheReadTokens,
            CacheWriteTokens = call.CacheWriteTokens,
            ReasoningTokens = call.ReasoningTokens,
            EstimatedTokens = call.EstimatedTokens,
            Credits = call.Credits,
            Activity = call.Activity,
            Outcome = call.Outcome,
            ContextParts = call.ContextParts
                .OrderBy(part => part.Kind)
                .ThenBy(part => part.Label)
                .Select(part => new AgentUsageContextPartRecord
                {
                    Id = part.Id,
                    CallId = part.CallId,
                    Kind = part.Kind,
                    Label = part.Label,
                    Role = part.Role,
                    Tool = part.Tool,
                    Integration = part.Integration,
                    Tokens = part.Tokens,
                    EstimatedTokens = part.EstimatedTokens,
                    CharacterCount = part.CharacterCount,
                })
                .ToList(),
        });

    private static AgentUsageCallRecord ToRecord(AgentUsageCallEntity call) => new()
    {
        Id = call.Id,
        AgentId = call.AgentId,
        WorkspaceId = call.WorkspaceId,
        OwnerId = call.OwnerId,
        RunId = call.RunId,
        ParentRunId = call.ParentRunId,
        CorrelationId = call.CorrelationId,
        Time = call.Time,
        Provider = call.Provider,
        Model = call.Model,
        DurationMs = call.DurationMs,
        InputTokens = call.InputTokens,
        OutputTokens = call.OutputTokens,
        CacheReadTokens = call.CacheReadTokens,
        CacheWriteTokens = call.CacheWriteTokens,
        ReasoningTokens = call.ReasoningTokens,
        EstimatedTokens = call.EstimatedTokens,
        Credits = call.Credits,
        Activity = call.Activity,
        Outcome = call.Outcome,
        ContextParts = call.ContextParts.Select(part => new AgentUsageContextPartRecord
        {
            Id = part.Id,
            CallId = part.CallId,
            Kind = part.Kind,
            Label = part.Label,
            Role = part.Role,
            Tool = part.Tool,
            Integration = part.Integration,
            Tokens = part.Tokens,
            EstimatedTokens = part.EstimatedTokens,
            CharacterCount = part.CharacterCount,
        }).ToList(),
    };
}
