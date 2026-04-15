namespace EnterpriseAgentOs.Api.Entities.RateLimiting;

public sealed record RateLimitDecision(bool Allowed, int RetryAfterSeconds);

public interface IRateLimitService
{
    /// <summary>
    /// Checks and increments the general skill-exec rate limit for the agent.
    /// Returns a decision indicating whether the call is allowed.
    /// </summary>
    Task<RateLimitDecision> CheckSkillExecAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Checks and increments the email-specific rate limit for the agent.
    /// Returns a decision indicating whether the call is allowed.
    /// </summary>
    Task<RateLimitDecision> CheckEmailAsync(Guid agentId, CancellationToken ct = default);
}
