namespace EnterpriseAgentOs.Application.Features.Management;

internal sealed class BillingGuard : IBillingGuard
{
    private readonly IDistributedCache _cache;
    private readonly IAgentRepository _agentRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ILogger<BillingGuard> _logger;
    private readonly BillingPolicyConfig _policy;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    public BillingGuard(
        IDistributedCache cache,
        IAgentRepository agentRepo,
        IUserSubscriptionRepository subRepo,
        ILogger<BillingGuard> logger,
        BillingPolicyConfig policy)
    {
        _cache = cache;
        _agentRepository = agentRepo;
        _subscriptionRepository = subRepo;
        _logger = logger;
        _policy = policy;
    }

    public async Task<BillingQuotaCheckResult> CheckQuotaAsync(Guid agentId, CancellationToken ct = default)
    {
        if (!_policy.EnforceUsageLimits)
            return BillingQuotaCheckResult.Skipped("Usage limits are disabled for this environment.");

        var cached = await _cache.GetStringAsync($"billing_status:{agentId}", ct);
        if (cached is not null)
        {
            return cached == "limit_reached"
                ? BillingQuotaCheckResult.ExceededLimit($"Agent {agentId} has reached the credit limit for this billing period.")
                : BillingQuotaCheckResult.Allowed();
        }

        return await RefreshAndCheckAsync(agentId, ct);
    }

    public async Task RefreshCacheAsync(Guid agentId, CancellationToken ct = default)
    {
        if (!_policy.EnforceUsageLimits)
        {
            await _cache.SetStringAsync($"billing_status:{agentId}", "ok", CacheOptions, ct);
            return;
        }

        await RefreshAndCheckAsync(agentId, ct);
    }

    private async Task<BillingQuotaCheckResult> RefreshAndCheckAsync(Guid agentId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
        if (agent is null)
            throw new InvalidOperationException($"Cannot check billing because agent {agentId} was not found.");
        if (agent.OwnerId is null)
            throw new InvalidOperationException($"Cannot check billing because agent {agentId} has no owner.");

        var sub = await _subscriptionRepository.GetByAsync(new UserSubscriptionFilter { UserId = agent.OwnerId.Value }, ct);
        if (sub is null)
        {
            sub = UserSubscription.CreateDefaultFree(agent.OwnerId.Value);
            await _subscriptionRepository.AddAsync(sub, ct);
            _logger.LogWarning(
                "Created missing free subscription during billing guard check for agent {AgentId} user {UserId}",
                agentId, agent.OwnerId.Value);
        }

        var budget = sub.CheckBudget();
        var exceeded = budget.OverBudget && !sub.OverageEnabled;
        var status = exceeded ? "limit_reached" : "ok";

        await _cache.SetStringAsync($"billing_status:{agentId}", status, CacheOptions, ct);

        if (exceeded)
            _logger.LogWarning("Agent {AgentId} (user {UserId}) quota exceeded: {Used}/{Budget} credits",
                agentId, agent.OwnerId, sub.CreditsUsedThisMonth, sub.CreditBudgetPerMonth);

        return exceeded
            ? BillingQuotaCheckResult.ExceededLimit($"Agent {agentId} has reached the credit limit for this billing period.")
            : BillingQuotaCheckResult.Allowed();
    }
}
