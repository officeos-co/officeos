using EnterpriseAgentOs.Application.Features.Management;
using EnterpriseAgentOs.Domain.Common.Services;
using EnterpriseAgentOs.Domain.Common.ValueObjects;
using EnterpriseAgentOs.Domain.Features.Agents;
using EnterpriseAgentOs.Domain.Features.Management;
using EnterpriseAgentOs.Infrastructure.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Billing;

public sealed class CreditRecordingServiceTests
{
    [Fact]
    public async Task RecordCreditUsageAsync_persists_user_usage_with_model_cost_weight()
    {
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agents = new FakeAgentRepository(Agent(agentId, ownerId));
        var subscriptions = new FakeUserSubscriptionRepository(UserSubscription.CreateDefaultFree(ownerId));
        var stripe = new FakeStripeMeteringService();
        var service = CreateService(agents, subscriptions, stripe);

        await service.RecordCreditUsageAsync(agentId, "gpt-4o", rawTokens: 10, CancellationToken.None);

        Assert.Equal(10 * ProviderRegistry.GetCostWeight("gpt-4o"), subscriptions.Current!.CreditsUsedThisMonth);
        Assert.Equal(1, subscriptions.UpdateCount);
        Assert.Empty(stripe.Events);
    }

    [Fact]
    public async Task RecordCreditUsageAsync_uses_custom_provider_cost_weight_for_configured_model()
    {
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var subscriptions = new FakeUserSubscriptionRepository(UserSubscription.CreateDefaultFree(ownerId));
        var service = CreateService(
            new FakeAgentRepository(Agent(agentId, ownerId)),
            subscriptions,
            new FakeStripeMeteringService(),
            new CustomLlmProviderConfig
            {
                BaseUrl = "http://localhost:8000/v1",
                ModelId = "deepseek-r1:8b",
                CostWeight = 4,
            });

        await service.RecordCreditUsageAsync(agentId, "deepseek-r1:8b", rawTokens: 10, CancellationToken.None);

        Assert.Equal(40, subscriptions.Current!.CreditsUsedThisMonth);
    }

    [Fact]
    public async Task RecordCreditUsageAsync_creates_missing_free_subscription_and_records_usage()
    {
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var subscriptions = new FakeUserSubscriptionRepository();
        var service = CreateService(new FakeAgentRepository(Agent(agentId, ownerId)), subscriptions, new FakeStripeMeteringService());

        await service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 25, CancellationToken.None);

        Assert.NotNull(subscriptions.Current);
        Assert.Equal(SubscriptionPlan.Free, subscriptions.Current!.Plan);
        Assert.Equal(25, subscriptions.Current.CreditsUsedThisMonth);
        Assert.Equal(1, subscriptions.AddCount);
    }

    [Fact]
    public async Task RecordCreditUsageAsync_fires_incremental_stripe_overage_for_subscription_state()
    {
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.Plan = SubscriptionPlan.Pro;
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 90;
        sub.OverageEnabled = true;
        sub.StripeCustomerId = "cus_test";
        sub.StripeOverageItemId = "si_overage";

        var stripe = new FakeStripeMeteringService();
        var service = CreateService(new FakeAgentRepository(Agent(agentId, ownerId)), new FakeUserSubscriptionRepository(sub), stripe);

        await service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 25, CancellationToken.None);

        var e = Assert.Single(stripe.Events);
        Assert.Equal("pro_credits_used", e.EventName);
        Assert.Equal("cus_test", e.CustomerId);
        Assert.Equal(15, e.Credits);
    }

    [Fact]
    public async Task RecordCreditUsageAsync_rejects_overage_without_stripe_metering_state()
    {
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var sub = UserSubscription.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 100;
        sub.OverageEnabled = true;

        var stripe = new FakeStripeMeteringService();
        var service = CreateService(new FakeAgentRepository(Agent(agentId, ownerId)), new FakeUserSubscriptionRepository(sub), stripe);

        await Assert.ThrowsAsync<BillingProviderException>(
            () => service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 1, CancellationToken.None));
        Assert.Empty(stripe.Events);
    }

    [Fact]
    public async Task RecordCreditUsageAsync_refuses_unowned_agents()
    {
        var agentId = Guid.NewGuid();
        var service = CreateService(new FakeAgentRepository(Agent(agentId, null)), new FakeUserSubscriptionRepository(), new FakeStripeMeteringService());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 1, CancellationToken.None));
    }

    private static CreditRecordingService CreateService(
        IAgentRepository agents,
        IUserSubscriptionRepository subscriptions,
        IStripeMeteringService stripe,
        CustomLlmProviderConfig? customLlmProviderConfig = null)
        => new(
            new StripeConfig(),
            agents,
            subscriptions,
            stripe,
            NullLogger<CreditRecordingService>.Instance,
            customLlmProviderConfig);

    private static AgentRecord Agent(Guid id, Guid? ownerId) => new()
    {
        Id = id,
        Name = "Test agent",
        Provider = "openai",
        Model = "gpt-4o-mini",
        OwnerId = ownerId,
        PodName = "pod",
    };

    private sealed class FakeStripeMeteringService : IStripeMeteringService
    {
        public List<(string EventName, string CustomerId, long Credits)> Events { get; } = [];

        public Task FireMeterEventAsync(string eventName, string customerId, long credits, CancellationToken ct = default)
        {
            Events.Add((eventName, customerId, credits));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserSubscriptionRepository : IUserSubscriptionRepository
    {
        public FakeUserSubscriptionRepository(UserSubscription? current = null) => Current = current;

        public UserSubscription? Current { get; private set; }
        public int AddCount { get; private set; }
        public int UpdateCount { get; private set; }

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
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeAgentRepository : IAgentRepository
    {
        private readonly AgentRecord? _agent;

        public FakeAgentRepository(AgentRecord? agent) => _agent = agent;

        public Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AgentRecord>>(_agent is null ? [] : [_agent]);

        public Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default)
            => Task.FromResult(
                _agent is not null
                && (!filter.Id.HasValue || _agent.Id == filter.Id.Value)
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
