namespace EnterpriseAgentOs.Api.Entities.RateLimiting;

public sealed class RateLimitService : IRateLimitService
{
    private const string SkillExecBucket = "skill_exec";
    private const string EmailBucket = "email";

    private readonly IRateLimitRepository _repository;
    private readonly EnterpriseAgentOs.Api.Properties.RateLimitingConfig _config;

    public RateLimitService(IRateLimitRepository repository, EnterpriseAgentOs.Api.Properties.RateLimitingConfig config)
    {
        _repository = repository;
        _config = config;
    }

    /// <inheritdoc />
    public async Task<RateLimitDecision> CheckSkillExecAsync(Guid agentId, CancellationToken ct = default)
    {
        var windowStart = ComputeWindowStart();
        var count = await _repository.IncrementAsync(agentId, SkillExecBucket, windowStart, ct);

        if (count > _config.SkillExecPerAgentPerHour)
        {
            return new RateLimitDecision(Allowed: false, RetryAfterSeconds: RetryAfter(windowStart));
        }

        return new RateLimitDecision(Allowed: true, RetryAfterSeconds: 0);
    }

    /// <inheritdoc />
    public async Task<RateLimitDecision> CheckEmailAsync(Guid agentId, CancellationToken ct = default)
    {
        var windowStart = ComputeWindowStart();
        var count = await _repository.IncrementAsync(agentId, EmailBucket, windowStart, ct);

        if (count > _config.EmailPerAgentPerHour)
        {
            return new RateLimitDecision(Allowed: false, RetryAfterSeconds: RetryAfter(windowStart));
        }

        return new RateLimitDecision(Allowed: true, RetryAfterSeconds: 0);
    }

    /// <summary>
    /// Computes the window start time by truncating the current UTC time to
    /// the nearest window boundary (WindowSeconds).
    /// </summary>
    private DateTime ComputeWindowStart()
    {
        var now = DateTime.UtcNow;
        var windowSeconds = _config.WindowSeconds > 0 ? _config.WindowSeconds : 3600;
        var totalSeconds = (long)now.Subtract(DateTime.UnixEpoch).TotalSeconds;
        var windowStart = totalSeconds - (totalSeconds % windowSeconds);
        return DateTime.UnixEpoch.AddSeconds(windowStart);
    }

    /// <summary>
    /// Computes how many seconds remain until the current window expires.
    /// </summary>
    private int RetryAfter(DateTime windowStart)
    {
        var windowEnd = windowStart.AddSeconds(_config.WindowSeconds > 0 ? _config.WindowSeconds : 3600);
        var remaining = (int)(windowEnd - DateTime.UtcNow).TotalSeconds;
        return remaining > 0 ? remaining : 1;
    }
}
