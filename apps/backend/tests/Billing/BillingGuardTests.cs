using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Management;
using OffceOs.Application.Features.Management;
using OffceOs.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Billing;

public sealed class BillingGuardTests
{
    [Fact]
    public async Task CheckQuotaAsync_creates_missing_free_subscription_before_allowing_usage()
    {
        var ownerId = Guid.NewGuid();
        var subscriptions = new FakeUserSubscriptionRepository();
        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), ownerId), subscriptions);

        var result = await guard.CheckQuotaAsync(agentId, CancellationToken.None);

        Assert.True(result.Enforced);
        Assert.False(result.Exceeded);
        Assert.NotNull(subscriptions.Current);
        Assert.Equal(SubscriptionPlan.Free, subscriptions.Current!.Plan);
        Assert.Equal(1, subscriptions.AddCount);
    }

    [Fact]
    public async Task CheckQuotaAsync_reports_exceeded_when_budget_exceeded_and_overage_disabled()
    {
        var ownerId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 101;

        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), ownerId), new FakeUserSubscriptionRepository(sub));

        var result = await guard.CheckQuotaAsync(agentId, CancellationToken.None);

        Assert.True(result.Enforced);
        Assert.True(result.Exceeded);
        Assert.Contains("credit limit", result.Reason);
    }

    [Fact]
    public async Task Development_environment_does_not_check_or_block_usage_limits()
    {
        var ownerId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 101;
        var subscriptions = new FakeUserSubscriptionRepository(sub);

        var (guard, agentId) = CreateGuard(
            Agent(Guid.NewGuid(), ownerId),
            subscriptions,
            new BillingPolicyConfig { EnforceUsageLimits = false });

        var result = await guard.CheckQuotaAsync(agentId, CancellationToken.None);

        Assert.False(result.Enforced);
        Assert.False(result.Exceeded);
        Assert.Contains("disabled", result.Reason);
        Assert.Equal(0, subscriptions.GetCount);
    }

    [Fact]
    public async Task Production_environment_checks_and_blocks_usage_limits()
    {
        var ownerId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 101;
        var subscriptions = new FakeUserSubscriptionRepository(sub);

        var (guard, agentId) = CreateGuard(
            Agent(Guid.NewGuid(), ownerId),
            subscriptions,
            new BillingPolicyConfig { EnforceUsageLimits = true });

        var result = await guard.CheckQuotaAsync(agentId, CancellationToken.None);

        Assert.True(result.Enforced);
        Assert.True(result.Exceeded);
        Assert.Equal(1, subscriptions.GetCount);
    }

    [Fact]
    public async Task CheckQuotaAsync_allows_overage_enabled_subscriptions()
    {
        var ownerId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 500;
        sub.OverageEnabled = true;

        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), ownerId), new FakeUserSubscriptionRepository(sub));

        var result = await guard.CheckQuotaAsync(agentId, CancellationToken.None);

        Assert.True(result.Enforced);
        Assert.False(result.Exceeded);
    }

    [Fact]
    public async Task CheckQuotaAsync_refuses_unowned_agents()
    {
        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), null), new FakeUserSubscriptionRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.CheckQuotaAsync(agentId, CancellationToken.None));
    }

    private static (BillingGuard Guard, Guid AgentId) CreateGuard(
        AgentRecord agent,
        FakeUserSubscriptionRepository subscriptions,
        BillingPolicyConfig? policy = null)
        => (new BillingGuard(
            new InMemoryDistributedCache(),
            new FakeAgentRepository(agent),
            subscriptions,
            NullLogger<BillingGuard>.Instance,
            policy ?? new BillingPolicyConfig()),
            agent.Id);

    private static AgentRecord Agent(Guid id, Guid? ownerId) => new()
    {
        Id = id,
        Name = "Test agent",
        Provider = "openai",
        Model = "gpt-4o-mini",
        OwnerId = ownerId,
    };

    private sealed class InMemoryDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public byte[]? Get(string key) => _values.GetValueOrDefault(key);
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _values[key] = value;
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserSubscriptionRepository : IUserSubscriptionRepository
    {
        public FakeUserSubscriptionRepository(UserSubscription? current = null) => Current = current;

        public UserSubscription? Current { get; private set; }
        public int AddCount { get; private set; }
        public int GetCount { get; private set; }

        public Task<UserSubscription?> GetByAsync(UserSubscriptionFilter filter, CancellationToken ct = default)
        {
            GetCount++;
            return Task.FromResult(
                Current is not null
                && (!filter.Id.HasValue || Current.Id == filter.Id.Value)
                && (!filter.UserId.HasValue || Current.UserId == filter.UserId.Value)
                    ? Current
                    : null);
        }

        public Task AddAsync(UserSubscription sub, CancellationToken ct = default)
        {
            Current = sub;
            AddCount++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UserSubscription sub, CancellationToken ct = default)
        {
            Current = sub;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeAgentRepository : IAgentRepository
    {
        private readonly AgentRecord _agent;

        public FakeAgentRepository(AgentRecord agent) => _agent = agent;

        public Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AgentRecord>>([_agent]);
        public Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default)
            => Task.FromResult(
                (!filter.Id.HasValue || _agent.Id == filter.Id.Value)
                && (!filter.OwnerId.HasValue || _agent.OwnerId == filter.OwnerId.Value)
                    ? _agent
                    : null);
        public Task AddAsync(AgentRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AgentRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> SoftDeleteAsync(AgentFilter filter, CancellationToken ct = default) => Task.FromResult(false);
        public Task UpdateStatusAsync(AgentFilter filter, AgentStatus status, CancellationToken ct = default) => Task.CompletedTask;
        public Task HardDeleteAsync(AgentFilter filter, CancellationToken ct = default) => Task.CompletedTask;
    }

}
