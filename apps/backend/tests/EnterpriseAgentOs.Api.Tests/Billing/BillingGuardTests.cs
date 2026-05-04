using EnterpriseAgentOs.Domain.Common.ValueObjects;
using EnterpriseAgentOs.Domain.Features.Agents;
using EnterpriseAgentOs.Domain.Features.Management;
using EnterpriseAgentOs.Application.Features.Management;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Billing;

public sealed class BillingGuardTests
{
    [Fact]
    public async Task ThrowIfQuotaExceededAsync_creates_missing_free_subscription_before_allowing_usage()
    {
        var ownerId = Guid.NewGuid();
        var subscriptions = new FakeUserSubscriptionRepository();
        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), ownerId), subscriptions);

        await guard.ThrowIfQuotaExceededAsync(agentId, CancellationToken.None);

        Assert.NotNull(subscriptions.Current);
        Assert.Equal(SubscriptionPlan.Free, subscriptions.Current!.Plan);
        Assert.Equal(1, subscriptions.AddCount);
    }

    [Fact]
    public async Task ThrowIfQuotaExceededAsync_blocks_when_budget_exceeded_and_overage_disabled()
    {
        var ownerId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 101;

        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), ownerId), new FakeUserSubscriptionRepository(sub));

        await Assert.ThrowsAsync<QuotaExceededException>(
            () => guard.ThrowIfQuotaExceededAsync(agentId, CancellationToken.None));
    }

    [Fact]
    public async Task ThrowIfQuotaExceededAsync_allows_overage_enabled_subscriptions()
    {
        var ownerId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 500;
        sub.OverageEnabled = true;

        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), ownerId), new FakeUserSubscriptionRepository(sub));

        await guard.ThrowIfQuotaExceededAsync(agentId, CancellationToken.None);
    }

    [Fact]
    public async Task ThrowIfQuotaExceededAsync_refuses_unowned_agents()
    {
        var (guard, agentId) = CreateGuard(Agent(Guid.NewGuid(), null), new FakeUserSubscriptionRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.ThrowIfQuotaExceededAsync(agentId, CancellationToken.None));
    }

    private static (BillingGuard Guard, Guid AgentId) CreateGuard(AgentRecord agent, FakeUserSubscriptionRepository subscriptions)
        => (new BillingGuard(new InMemoryDistributedCache(), new FakeAgentRepository(agent), subscriptions, NullLogger<BillingGuard>.Instance), agent.Id);

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

        public Task<UserSubscription?> GetByAsync(UserSubscriptionFilter filter, CancellationToken ct = default)
            => Task.FromResult(
                Current is not null
                && (!filter.Id.HasValue || Current.Id == filter.Id.Value)
                && (!filter.UserId.HasValue || Current.UserId == filter.UserId.Value)
                    ? Current
                    : null);

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
