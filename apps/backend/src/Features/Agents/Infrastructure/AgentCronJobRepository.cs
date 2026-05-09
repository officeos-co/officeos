namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class AgentCronJobRepository : IAgentCronJobRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentCronJobRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<AgentCronJobRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentCronJobs
            .Where(j => j.AgentId == agentId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToAgentCronJobRecord).ToList();
    }

    public async Task<IReadOnlyList<AgentCronJobWithAgentRecord>> ListForOwnerAsync(Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentCronJobs
            .AsNoTracking()
            .Join(
                _eaosDbContext.Agents.AsNoTracking(),
                job => job.AgentId,
                agent => agent.Id,
                (job, agent) => new { job, agent })
            .Where(row => row.agent.OwnerId == ownerId && !row.agent.IsDeleted);

        if (workspaceId.HasValue)
            query = query.Where(row => row.agent.WorkspaceId == workspaceId.Value);

        var rows = await query
            .OrderByDescending(row => row.job.CreatedAt)
            .ToListAsync(ct);

        return rows
            .Select(row => new AgentCronJobWithAgentRecord(ToAgentCronJobRecord(row.job), row.agent.Name))
            .ToList();
    }

    public async Task<IReadOnlyList<AgentCronJobRecord>> ListAllEnabledAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentCronJobs
            .Where(j => j.Enabled)
            .ToListAsync(ct);
        return entities.Select(ToAgentCronJobRecord).ToList();
    }

    public async Task<AgentCronJobRecord?> GetByAsync(AgentCronJobFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentCronJobs.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(j => j.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(j => j.AgentId == filter.AgentId.Value);

        if (filter.Enabled.HasValue)
            query = query.Where(j => j.Enabled == filter.Enabled.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToAgentCronJobRecord(entity);
    }

    public async Task<AgentCronJobWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentCronJobs
            .AsNoTracking()
            .Join(
                _eaosDbContext.Agents.AsNoTracking(),
                job => job.AgentId,
                agent => agent.Id,
                (job, agent) => new { job, agent })
            .Where(row => row.job.Id == id && row.agent.OwnerId == ownerId && !row.agent.IsDeleted);

        if (workspaceId.HasValue)
            query = query.Where(row => row.agent.WorkspaceId == workspaceId.Value);

        var row = await query
            .FirstOrDefaultAsync(ct);

        return row is null
            ? null
            : new AgentCronJobWithAgentRecord(ToAgentCronJobRecord(row.job), row.agent.Name);
    }

    public async Task<AgentCronJobRecord> CreateAsync(Guid agentId, string name, string expression, string prompt, CancellationToken ct = default)
    {
        var record = AgentCronJobRecord.Create(agentId, name, expression, prompt);

        // Compute initial NextRunAt
        try
        {
            var cron = Cronos.CronExpression.Parse(expression);
            var next = cron.GetNextOccurrence(DateTime.UtcNow, inclusive: false);
            if (next.HasValue) record.SetNextRun(next.Value);
        }
        catch (Cronos.CronFormatException) { /* will be caught later by scheduler */ }

        _eaosDbContext.AgentCronJobs.Add(ToAgentCronJobEntity(record));
        await _eaosDbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task UpdateAsync(AgentCronJobRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentCronJobs.FirstOrDefaultAsync(j => j.Id == record.Id, ct);
        if (entity is null) return;
        MapToAgentCronJobEntity(record, entity);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentCronJobs.FirstOrDefaultAsync(j => j.Id == id, ct)
            ?? throw new InvalidOperationException("Cron job not found");
        entity.Enabled = enabled;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentCronJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null) return false;
        _eaosDbContext.AgentCronJobs.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    private static AgentCronJobRecord ToAgentCronJobRecord(AgentCronJobEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        Name = e.Name,
        Expression = new Domain.Common.ValueObjects.CronExpression(e.Expression),
        Prompt = e.Prompt,
        Enabled = e.Enabled,
        LastRunAt = e.LastRunAt,
        NextRunAt = e.NextRunAt,
        CreatedAt = e.CreatedAt,
    };

    private static AgentCronJobEntity ToAgentCronJobEntity(AgentCronJobRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        Name = r.Name,
        Expression = (string)r.Expression,
        Prompt = r.Prompt,
        Enabled = r.Enabled,
        LastRunAt = r.LastRunAt,
        NextRunAt = r.NextRunAt,
        CreatedAt = r.CreatedAt,
    };

    private static void MapToAgentCronJobEntity(AgentCronJobRecord r, AgentCronJobEntity e)
    {
        e.Name = r.Name;
        e.Expression = (string)r.Expression;
        e.Prompt = r.Prompt;
        e.Enabled = r.Enabled;
        e.LastRunAt = r.LastRunAt;
        e.NextRunAt = r.NextRunAt;
    }
}
