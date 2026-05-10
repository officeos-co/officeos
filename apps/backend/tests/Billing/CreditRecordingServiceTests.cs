using OffceOs.Domain.Common.Services;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Billing;
using OffceOs.Configuration;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Billing;

public sealed class CreditRecordingServiceTests
{
    [Fact]
    public async Task RecordCreditUsageAsync_persists_user_usage_with_model_cost_weight()
    {
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agents = new FakeAgentRepository(AgentRecordFactory.Agent(agentId, ownerId));
        var subscriptions = new FakeUserSubscriptionRepository(UserSubscriptionRecord.CreateDefaultFree(ownerId));
        var stripe = new FakeStripeMeteringService();
        var service = CreditRecordingServiceTestFactory.CreateService(agents, subscriptions, stripe);

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
        var subscriptions = new FakeUserSubscriptionRepository(UserSubscriptionRecord.CreateDefaultFree(ownerId));
        var service = CreditRecordingServiceTestFactory.CreateService(
            new FakeAgentRepository(AgentRecordFactory.Agent(agentId, ownerId)),
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
        var service = CreditRecordingServiceTestFactory.CreateService(new FakeAgentRepository(AgentRecordFactory.Agent(agentId, ownerId)), subscriptions, new FakeStripeMeteringService());

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
        var sub = UserSubscriptionRecord.CreateDefaultFree(ownerId);
        sub.Plan = SubscriptionPlan.Pro;
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 90;
        sub.OverageEnabled = true;
        sub.StripeCustomerId = "cus_test";
        sub.StripeOverageItemId = "si_overage";

        var stripe = new FakeStripeMeteringService();
        var service = CreditRecordingServiceTestFactory.CreateService(new FakeAgentRepository(AgentRecordFactory.Agent(agentId, ownerId)), new FakeUserSubscriptionRepository(sub), stripe);

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
        var sub = UserSubscriptionRecord.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 100;
        sub.OverageEnabled = true;

        var stripe = new FakeStripeMeteringService();
        var service = CreditRecordingServiceTestFactory.CreateService(new FakeAgentRepository(AgentRecordFactory.Agent(agentId, ownerId)), new FakeUserSubscriptionRepository(sub), stripe);

        await Assert.ThrowsAsync<BillingProviderException>(
            () => service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 1, CancellationToken.None));
        Assert.Empty(stripe.Events);
    }

    [Fact]
    public async Task RecordCreditUsageAsync_refuses_unowned_agents()
    {
        var agentId = Guid.NewGuid();
        var service = CreditRecordingServiceTestFactory.CreateService(new FakeAgentRepository(AgentRecordFactory.Agent(agentId, null)), new FakeUserSubscriptionRepository(), new FakeStripeMeteringService());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 1, CancellationToken.None));
    }

}
