using Cronos;
using EnterpriseAgentOs.Domain.Interfaces.Agents;
using EnterpriseAgentOs.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentCronJobRepository : IAgentCronJobRepository
{
    private readonly EaosDbContext _db;

    public AgentCronJobRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<AgentCronJobRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
        => await _db.AgentCronJobs
            .Where(j => j.AgentId == agentId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AgentCronJobRecord>> ListAllEnabledAsync(CancellationToken ct = default)
        => await _db.AgentCronJobs
            .Where(j => j.Enabled)
            .ToListAsync(ct);

    public async Task<AgentCronJobRecord?> GetAsync(Guid id, CancellationToken ct = default)
        => await _db.AgentCronJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

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

        _db.AgentCronJobs.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task UpdateAsync(AgentCronJobRecord record, CancellationToken ct = default)
    {
        _db.AgentCronJobs.Update(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default)
    {
        var record = await _db.AgentCronJobs.FirstOrDefaultAsync(j => j.Id == id, ct)
            ?? throw new InvalidOperationException("Cron job not found");
        record.SetEnabled(enabled);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _db.AgentCronJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (record is null) return false;
        _db.AgentCronJobs.Remove(record);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
