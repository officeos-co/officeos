namespace EnterpriseAgentOs.Domain.Features.Billing;

public interface IBillingGuard
{
    /// <summary>Returns true when the agent's owner has exceeded their credit budget.</summary>
    Task<bool> IsQuotaExceededAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>Throws <see cref="QuotaExceededException"/> when the quota is exceeded.</summary>
    Task ThrowIfQuotaExceededAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>Refreshes the Redis cache after credits are recorded.</summary>
    Task RefreshCacheAsync(Guid agentId, CancellationToken ct = default);
}

public sealed class QuotaExceededException : Exception
{
    public QuotaExceededException(string message) : base(message) { }
}
