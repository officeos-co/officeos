using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Application.Features.Management;

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
        var cached = await _cache.GetStringAsync($"billing_status:{agentId}", ct);
        if (cached is not null)
            return cached == "limit_reached";

        return await RefreshAndCheckAsync(agentId, ct);
    }

    public async Task ThrowIfQuotaExceededAsync(Guid agentId, CancellationToken ct = default)
    {
        if (await RefreshAndCheckAsync(agentId, ct))
            throw new QuotaExceededException($"Agent {agentId} has reached the credit limit for this billing period.");
    }

    public async Task RefreshCacheAsync(Guid agentId, CancellationToken ct = default)
    {
        await RefreshAndCheckAsync(agentId, ct);
    }

    private async Task<bool> RefreshAndCheckAsync(Guid agentId, CancellationToken ct)
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

        return exceeded;
    }
}
