namespace EnterpriseAgentOs.Domain.Models;

/// <summary>
/// DB-backed sliding window counter for per-agent rate limiting.
/// One row per (AgentId, BucketKey, WindowStart) window.
/// </summary>
public sealed class AgentRateLimitRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid AgentId { get; set; }

    /// <summary>Bucket identifier, e.g. "skill_exec" or "email".</summary>
    [Required, MaxLength(64)]
    public string BucketKey { get; set; } = string.Empty;

    /// <summary>Start of the current rate-limit window (UTC, truncated to WindowSeconds).</summary>
    public DateTime WindowStart { get; set; }

    /// <summary>Number of executions in the current window.</summary>
    public int Count { get; set; }
}
