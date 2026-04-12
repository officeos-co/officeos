using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Entities.Billing;
using EnterpriseAgentOs.Api.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseAgentOs.Api.Tests.Billing;

/// <summary>
/// Unit tests for UserSubscription and the StripeService user-subscription methods.
/// Tests that exercise live Stripe SDK calls (CreateUserCheckoutSessionAsync) are
/// expected to throw when no real API key is configured.
/// </summary>
public sealed class UserSubscriptionTests
{
    private static EaosDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EaosDbContext(opts);
    }

    private static StripeService CreateService(
        string proMonthlyPriceId = "price_pro_monthly",
        string proYearlyPriceId = "price_pro_yearly") =>
        new(
            new StripeConfig
            {
                SecretKey = "STRIPE_SECRET_KEY_PLACEHOLDER",
                WebhookSecret = "STRIPE_WEBHOOK_SECRET_PLACEHOLDER",
                FreePriceId = "price_free",
                TeamPriceId = "price_team",
                TeamOveragePriceId = "price_team_overage",
                ProMonthlyPriceId = proMonthlyPriceId,
                ProYearlyPriceId = proYearlyPriceId,
                Enabled = false,
            },
            new FrontendConfig("https://dashboard.harrokrog.com"),
            CreateDb(),
            NullLogger<StripeService>.Instance);

    // -------------------------------------------------------------------------
    // Default subscription for a new user
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserSubscription_NewUser_ReturnsFreePlan()
    {
        var svc = CreateService();

        var sub = await svc.GetUserSubscriptionAsync(Guid.NewGuid());

        Assert.Equal("free", sub.Plan);
    }

    [Fact]
    public async Task GetUserSubscription_NewUser_IsActive()
    {
        var svc = CreateService();

        var sub = await svc.GetUserSubscriptionAsync(Guid.NewGuid());

        Assert.True(sub.IsActive);
    }

    // -------------------------------------------------------------------------
    // Free plan limits match PlanLimits.IndividualFree
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserSubscription_FreePlan_LimitsMatchIndividualFree()
    {
        var svc = CreateService();

        var sub = await svc.GetUserSubscriptionAsync(Guid.NewGuid());

        Assert.Equal(PlanLimits.IndividualFree.ConcurrentAgents, sub.ConcurrentAgentLimit);
        Assert.Equal(PlanLimits.IndividualFree.TokensPerMonth, sub.TokenBudgetPerMonth);
    }

    [Fact]
    public async Task GetUserSubscription_FreePlan_HasOneAgent()
    {
        var svc = CreateService();

        var sub = await svc.GetUserSubscriptionAsync(Guid.NewGuid());

        Assert.Equal(1, sub.ConcurrentAgentLimit);
    }

    [Fact]
    public async Task GetUserSubscription_FreePlan_HasTwoMillionTokens()
    {
        var svc = CreateService();

        var sub = await svc.GetUserSubscriptionAsync(Guid.NewGuid());

        Assert.Equal(2_000_000L, sub.TokenBudgetPerMonth);
    }

    // -------------------------------------------------------------------------
    // CreateUserCheckoutSessionAsync — requires live Stripe API key
    // These tests verify the method throws gracefully with placeholder keys.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUserCheckoutSession_ProMonthly_ThrowsWithPlaceholderKey()
    {
        var svc = CreateService(proMonthlyPriceId: "price_pro_monthly_test");
        var userId = Guid.NewGuid();

        // With a placeholder key the Stripe API call will throw (no real auth)
        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.CreateUserCheckoutSessionAsync(userId, "user@example.com", "pro", "monthly"));
    }

    [Fact]
    public async Task CreateUserCheckoutSession_ProYearly_ThrowsWithPlaceholderKey()
    {
        var svc = CreateService(proYearlyPriceId: "price_pro_yearly_test");
        var userId = Guid.NewGuid();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.CreateUserCheckoutSessionAsync(userId, "user@example.com", "pro", "yearly"));
    }

    // -------------------------------------------------------------------------
    // CheckUserTokenBudgetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CheckUserTokenBudget_FreshUser_ReturnsFullBudget()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();

        var (remaining, overBudget) = await svc.CheckUserTokenBudgetAsync(userId);

        Assert.Equal(2_000_000L, remaining);
        Assert.False(overBudget);
    }

    [Fact]
    public async Task CheckUserTokenBudget_FreshUser_NotOverBudget()
    {
        var svc = CreateService();

        var (_, overBudget) = await svc.CheckUserTokenBudgetAsync(Guid.NewGuid());

        Assert.False(overBudget);
    }
}
