namespace EnterpriseAgentOs.Api.Entities.RateLimiting;

public sealed class RateLimitRepository : IRateLimitRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public RateLimitRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<int> IncrementAsync(
        Guid agentId,
        string bucketKey,
        DateTime windowStart,
        CancellationToken ct = default)
    {
        var row = await _db.AgentRateLimits
            .FirstOrDefaultAsync(
                r => r.AgentId == agentId
                     && r.BucketKey == bucketKey
                     && r.WindowStart == windowStart,
                ct);

        if (row is null)
        {
            row = new EnterpriseAgentOs.Api.Database.Models.AgentRateLimitRecord
            {
                AgentId = agentId,
                BucketKey = bucketKey,
                WindowStart = windowStart,
                Count = 1,
            };
            _db.AgentRateLimits.Add(row);
        }
        else
        {
            row.Count++;
        }

        await _db.SaveChangesAsync(ct);
        return row.Count;
    }
}
