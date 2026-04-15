using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.RateLimiting;

public interface IRateLimitRepository
{
    /// <summary>
    /// Atomically increments the counter for (agentId, bucketKey, windowStart).
    /// Creates the row if it doesn't exist. Returns the updated count.
    /// </summary>
    Task<int> IncrementAsync(Guid agentId, string bucketKey, DateTime windowStart, CancellationToken ct = default);
}
