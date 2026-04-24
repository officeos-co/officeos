using Cronos;

namespace EnterpriseAgentOs.Infrastructure.Features.AgentCronJobs;

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

    public async Task<IReadOnlyList<AgentCronJobRecord>> ListAllEnabledAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentCronJobs
            .Where(j => j.Enabled)
            .ToListAsync(ct);
        return entities.Select(ToAgentCronJobRecord).ToList();
    }

    public async Task<AgentCronJobRecord?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentCronJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        return entity is null ? null : ToAgentCronJobRecord(entity);
    }

    public async Task<AgentCronJobRecord> CreateAsync(Guid agentId, string name, string expression, string prompt, CancellationToken ct = default)
    {
        var record = AgentCronJobRecord.Create(agentId, name, expression, prompt);

        // Compute initial NextRunAt
        try
        {
            var cron = CronExpression.Parse(expression);
            var next = cron.GetNextOccurrence(DateTime.UtcNow, inclusive: false);
            if (next.HasValue) record.SetNextRun(next.Value);
        }
        catch (CronFormatException) { /* will be caught later by scheduler */ }

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
        Expression = e.Expression,
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
        Expression = r.Expression,
        Prompt = r.Prompt,
        Enabled = r.Enabled,
        LastRunAt = r.LastRunAt,
        NextRunAt = r.NextRunAt,
        CreatedAt = r.CreatedAt,
    };

    private static void MapToAgentCronJobEntity(AgentCronJobRecord r, AgentCronJobEntity e)
    {
        e.Name = r.Name;
        e.Expression = r.Expression;
        e.Prompt = r.Prompt;
        e.Enabled = r.Enabled;
        e.LastRunAt = r.LastRunAt;
        e.NextRunAt = r.NextRunAt;
    }
}
