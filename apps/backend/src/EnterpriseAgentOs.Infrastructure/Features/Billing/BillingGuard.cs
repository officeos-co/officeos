using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Infrastructure.Features.Billing;

internal sealed class BillingGuard : IBillingGuard
{
    private readonly IDistributedCache _cache;
    private readonly IAgentRepository _agentRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ILogger<BillingGuard> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    public BillingGuard(
        IDistributedCache cache,
        IAgentRepository agentRepo,
        IUserSubscriptionRepository subRepo,
        ILogger<BillingGuard> logger)
    {
        _cache = cache;
        _agentRepository = agentRepo;
        _subscriptionRepository = subRepo;
        _logger = logger;
    }

    public async Task<bool> IsQuotaExceededAsync(Guid agentId, CancellationToken ct = default)
    {
        // Fast path: check Redis cache first
        var cached = await _cache.GetStringAsync($"billing_status:{agentId}", ct);
        if (cached is not null)
            return cached == "limit_reached";

        // Slow path: query DB and populate cache
        return await RefreshAndCheckAsync(agentId, ct);
    }

    public async Task ThrowIfQuotaExceededAsync(Guid agentId, CancellationToken ct = default)
    {
        if (await IsQuotaExceededAsync(agentId, ct))
            throw new QuotaExceededException($"Agent {agentId} has reached the credit limit for this billing period.");
    }

    public async Task RefreshCacheAsync(Guid agentId, CancellationToken ct = default)
    {
        await RefreshAndCheckAsync(agentId, ct);
    }

    private async Task<bool> RefreshAndCheckAsync(Guid agentId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetAsync(agentId, ct);
        if (agent?.OwnerId is null)
            return false;

        var sub = await _subscriptionRepository.GetByUserIdAsync(agent.OwnerId.Value, ct);
        if (sub is null)
            return false;

        var budget = sub.CheckBudget();
        var exceeded = budget.OverBudget && !sub.OverageEnabled;
        var status = exceeded ? "limit_reached" : "ok";

        await _cache.SetStringAsync($"billing_status:{agentId}", status, CacheOptions, ct);

        if (exceeded)
            _logger.LogWarning("Agent {AgentId} (user {UserId}) quota exceeded: {Used}/{Budget} credits",
                agentId, agent.OwnerId, sub.CreditsUsedThisMonth, sub.CreditBudgetPerMonth);

        return exceeded;
    }
}
