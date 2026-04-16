namespace EnterpriseAgentOs.Api.Entities.Runners;

public sealed class RunnerJobRepository : IRunnerJobRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public RunnerJobRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db) => _db = db;

    public async Task<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord> CreateAsync(Guid runnerId, string payload, TimeSpan claimTimeout, CancellationToken ct)
    {
        var job = new EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord
        {
            RunnerId = runnerId,
            Payload = payload,
            ClaimDeadline = DateTime.UtcNow.Add(claimTimeout),
        };
        _db.RunnerJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord?> ClaimNextAsync(Guid runnerId, CancellationToken ct)
    {
        var job = await _db.RunnerJobs
            .Where(j => j.RunnerId == runnerId && j.Status == "pending")
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (job is null) return null;

        job.Status = "running";
        job.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.RunnerJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task UpdateAsync(EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord job, CancellationToken ct)
    {
        _db.RunnerJobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord>> GetRecentByRunnerAsync(Guid runnerId, int limit, CancellationToken ct)
        => await _db.RunnerJobs
            .Where(j => j.RunnerId == runnerId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task FailStaleJobsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var stale = await _db.RunnerJobs
            .Where(j =>
                (j.Status == "pending" && j.ClaimDeadline < now) ||
                (j.Status == "running" && j.StartedAt != null && j.StartedAt.Value.AddSeconds(120) < now))
            .ToListAsync(ct);

        foreach (var job in stale)
        {
            job.Status = "failed";
            job.Result = "{\"error\":\"Job timed out\"}";
            job.CompletedAt = now;
        }

        if (stale.Count > 0)
            await _db.SaveChangesAsync(ct);
    }
}
