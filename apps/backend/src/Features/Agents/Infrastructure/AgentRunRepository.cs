namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class AgentRunRepository : IAgentRunRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentRunRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default)
    {
        var entity = await ToEntityAsync(run, ct);
        _eaosDbContext.AgentRuns.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<AgentRunRecord?> GetByAsync(AgentRunFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentRuns.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(r => r.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(r => r.AgentId == filter.AgentId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(r => r.WorkspaceId == filter.WorkspaceId.Value);

        if (filter.ParentRunId.HasValue)
            query = query.Where(r => r.ParentRunId == filter.ParentRunId.Value);

        if (!string.IsNullOrEmpty(filter.Kind))
            query = query.Where(r => r.Kind == filter.Kind);

        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(r => r.Status == filter.Status);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<AgentRunRecord>> ListAsync(AgentRunFilter filter, int limit = 100, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentRuns.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(r => r.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(r => r.AgentId == filter.AgentId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(r => r.WorkspaceId == filter.WorkspaceId.Value);

        if (filter.ParentRunId.HasValue)
            query = query.Where(r => r.ParentRunId == filter.ParentRunId.Value);

        if (!string.IsNullOrEmpty(filter.Kind))
            query = query.Where(r => r.Kind == filter.Kind);

        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(r => r.Status == filter.Status);

        var entities = await query
            .OrderBy(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentRuns.AsNoTracking().Where(r => r.AgentId == agentId);
        if (parentRunId.HasValue)
            query = query.Where(r => r.ParentRunId == parentRunId.Value);

        var entities = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentRuns.FirstOrDefaultAsync(r => r.Id == run.Id, ct);
        if (entity is null) return;

        entity.Status = run.Status;
        entity.Result = run.Result;
        entity.Error = run.Error;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.CompletedAt = run.CompletedAt;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static AgentRunRecord ToRecord(AgentRunEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        WorkspaceId = e.WorkspaceId,
        ParentRunId = e.ParentRunId,
        ParentCorrelationId = e.ParentCorrelationId,
        Kind = e.Kind,
        Status = e.Status,
        Name = e.Name,
        Description = e.Description,
        Prompt = e.Prompt,
        Result = e.Result,
        Error = e.Error,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        CompletedAt = e.CompletedAt,
    };

    private async Task<AgentRunEntity> ToEntityAsync(AgentRunRecord r, CancellationToken ct)
    {
        var workspaceId = r.WorkspaceId ?? await _eaosDbContext.Agents.AsNoTracking()
            .Where(a => a.Id == r.AgentId)
            .Select(a => a.WorkspaceId)
            .FirstOrDefaultAsync(ct);

        return new AgentRunEntity
        {
            Id = r.Id,
            AgentId = r.AgentId,
            WorkspaceId = workspaceId,
            ParentRunId = r.ParentRunId,
            ParentCorrelationId = r.ParentCorrelationId,
            Kind = r.Kind,
            Status = r.Status,
            Name = r.Name,
            Description = r.Description,
            Prompt = r.Prompt,
            Result = r.Result,
            Error = r.Error,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CompletedAt = r.CompletedAt,
        };
    }
}
