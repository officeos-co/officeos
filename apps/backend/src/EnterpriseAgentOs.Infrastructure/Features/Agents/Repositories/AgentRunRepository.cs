namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class AgentRunRepository : IAgentRunRepository
{
    private readonly EaosDbContext _db;

    public AgentRunRepository(EaosDbContext db) => _db = db;

    public async Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default)
    {
        _db.AgentRuns.Add(ToEntity(run));
        await _db.SaveChangesAsync(ct);
        return run;
    }

    public async Task<AgentRunRecord?> GetAsync(Guid runId, CancellationToken ct = default)
    {
        var entity = await _db.AgentRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default)
    {
        var query = _db.AgentRuns.AsNoTracking().Where(r => r.AgentId == agentId);
        if (parentRunId.HasValue)
            query = query.Where(r => r.ParentRunId == parentRunId.Value);

        var entities = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default)
    {
        var entity = await _db.AgentRuns.FirstOrDefaultAsync(r => r.Id == run.Id, ct);
        if (entity is null) return;

        entity.Status = run.Status;
        entity.Result = run.Result;
        entity.Error = run.Error;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.CompletedAt = run.CompletedAt;
        await _db.SaveChangesAsync(ct);
    }

    private static AgentRunRecord ToRecord(AgentRunEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
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

    private static AgentRunEntity ToEntity(AgentRunRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
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
