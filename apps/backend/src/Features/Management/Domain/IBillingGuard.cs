namespace OffceOs.Domain.Features.Management;

public interface IBillingGuard
{
    /// <summary>Checks whether usage is allowed for the agent's owner.</summary>
    Task<BillingQuotaCheckResult> CheckQuotaAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>Refreshes the Redis cache after credits are recorded.</summary>
    Task RefreshCacheAsync(Guid agentId, CancellationToken ct = default);
}

public sealed record BillingQuotaCheckResult(
    bool Enforced,
    bool Exceeded,
    string? Reason = null)
{
    public static BillingQuotaCheckResult Allowed() => new(true, false);

    public static BillingQuotaCheckResult ExceededLimit(string reason) => new(true, true, reason);

    public static BillingQuotaCheckResult Skipped(string reason) => new(false, false, reason);
}

public sealed class QuotaExceededException : Exception
{
    public QuotaExceededException(string message) : base(message) { }
}
