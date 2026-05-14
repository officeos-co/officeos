using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Billing;
using OffceOs.Configuration;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Billing;

public sealed class BillingGuardTests
{
    [Fact]
    public async Task CheckQuotaAsync_creates_missing_free_subscription_before_allowing_usage()
    {
        var ownerId = Guid.NewGuid();
        var subscriptions = new FakeUserSubscriptionRepository();
        var (guard, agentId) = BillingGuardTestFactory.CreateGuard(AgentRecordFactory.Agent(Guid.NewGuid(), ownerId), subscriptions);

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
        var sub = UserSubscriptionRecord.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 101;

        var (guard, agentId) = BillingGuardTestFactory.CreateGuard(AgentRecordFactory.Agent(Guid.NewGuid(), ownerId), new FakeUserSubscriptionRepository(sub));

        var result = await guard.CheckQuotaAsync(agentId, CancellationToken.None);

        Assert.True(result.Enforced);
        Assert.True(result.Exceeded);
        Assert.Contains("credit limit", result.Reason);
    }

    [Fact]
    public async Task Development_environment_does_not_check_or_block_usage_limits()
    {
        var ownerId = Guid.NewGuid();
        var sub = UserSubscriptionRecord.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 101;
        var subscriptions = new FakeUserSubscriptionRepository(sub);

        var (guard, agentId) = BillingGuardTestFactory.CreateGuard(
            AgentRecordFactory.Agent(Guid.NewGuid(), ownerId),
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
        var sub = UserSubscriptionRecord.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 101;
        var subscriptions = new FakeUserSubscriptionRepository(sub);

        var (guard, agentId) = BillingGuardTestFactory.CreateGuard(
            AgentRecordFactory.Agent(Guid.NewGuid(), ownerId),
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
        var sub = UserSubscriptionRecord.CreateDefaultFree(ownerId);
        sub.CreditBudgetPerMonth = 100;
        sub.CreditsUsedThisMonth = 500;
        sub.OverageEnabled = true;

        var (guard, agentId) = BillingGuardTestFactory.CreateGuard(AgentRecordFactory.Agent(Guid.NewGuid(), ownerId), new FakeUserSubscriptionRepository(sub));

        var result = await guard.CheckQuotaAsync(agentId, CancellationToken.None);

        Assert.True(result.Enforced);
        Assert.False(result.Exceeded);
    }

    [Fact]
    public async Task CheckQuotaAsync_refuses_unowned_agents()
    {
        var (guard, agentId) = BillingGuardTestFactory.CreateGuard(AgentRecordFactory.Agent(Guid.NewGuid(), null), new FakeUserSubscriptionRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.CheckQuotaAsync(agentId, CancellationToken.None));
    }
}
