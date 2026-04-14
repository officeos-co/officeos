using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Entities.Billing;
using EnterpriseAgentOs.Api.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseAgentOs.Api.Tests.Billing;

/// <summary>
/// Unit tests for UserBillingService — user subscription lookup, credit budgets, and checkout.
/// Tests that exercise live Stripe SDK calls (CreateCheckoutSessionAsync) are expected to throw
/// when no real API key is configured.
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

    private static UserBillingService CreateService(
        string proMonthlyPriceId = "price_pro_monthly",
        string proYearlyPriceId = "price_pro_yearly") =>
        new(
            new StripeConfig
            {
                SecretKey = "STRIPE_SECRET_KEY_PLACEHOLDER",
                WebhookSecret = "STRIPE_WEBHOOK_SECRET_PLACEHOLDER",
                FreePriceId = "price_free",
                TeamMonthlyPriceId = "price_team_monthly",
                TeamYearlyPriceId = "price_team_yearly",
                TeamOveragePriceId = "price_team_overage",
                FreeOveragePriceId = "price_free_overage",
                ProOveragePriceId = "price_pro_overage",
                ProMonthlyPriceId = proMonthlyPriceId,
                ProYearlyPriceId = proYearlyPriceId,
                Enabled = false,
            },
            new FrontendConfig("https://dashboard.officeos.co"),
            CreateDb(),
            NullLogger<UserBillingService>.Instance);

    // -------------------------------------------------------------------------
    // Default subscription for a new user
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserSubscription_NewUser_ReturnsFreePlan()
    {
        var svc = CreateService();

        var sub = await svc.GetSubscriptionAsync(Guid.NewGuid());

        Assert.Equal("free", sub.Plan);
    }

    [Fact]
    public async Task GetUserSubscription_NewUser_IsActive()
    {
        var svc = CreateService();

        var sub = await svc.GetSubscriptionAsync(Guid.NewGuid());

        Assert.True(sub.IsActive);
    }

    // -------------------------------------------------------------------------
    // Free plan limits match PlanLimits.IndividualFree
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserSubscription_FreePlan_LimitsMatchIndividualFree()
    {
        var svc = CreateService();

        var sub = await svc.GetSubscriptionAsync(Guid.NewGuid());

        Assert.Equal(PlanLimits.IndividualFree.ConcurrentAgents, sub.ConcurrentAgentLimit);
        Assert.Equal(PlanLimits.IndividualFree.CreditsPerMonth, sub.CreditBudgetPerMonth);
    }

    [Fact]
    public async Task GetUserSubscription_FreePlan_HasOneAgent()
    {
        var svc = CreateService();

        var sub = await svc.GetSubscriptionAsync(Guid.NewGuid());

        Assert.Equal(1, sub.ConcurrentAgentLimit);
    }

    [Fact]
    public async Task GetUserSubscription_FreePlan_Has500KCredits()
    {
        var svc = CreateService();

        var sub = await svc.GetSubscriptionAsync(Guid.NewGuid());

        Assert.Equal(500_000L, sub.CreditBudgetPerMonth);
    }

    // -------------------------------------------------------------------------
    // CreateCheckoutSessionAsync — requires live Stripe API key
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUserCheckoutSession_ProMonthly_ThrowsWithPlaceholderKey()
    {
        var svc = CreateService(proMonthlyPriceId: "price_pro_monthly_test");
        var userId = Guid.NewGuid();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.CreateCheckoutSessionAsync(userId, "user@example.com", "pro", "monthly"));
    }

    [Fact]
    public async Task CreateUserCheckoutSession_ProYearly_ThrowsWithPlaceholderKey()
    {
        var svc = CreateService(proYearlyPriceId: "price_pro_yearly_test");
        var userId = Guid.NewGuid();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.CreateCheckoutSessionAsync(userId, "user@example.com", "pro", "yearly"));
    }

    // -------------------------------------------------------------------------
    // CheckCreditBudgetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CheckCreditBudget_FreshUser_ReturnsFullBudget()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();

        var (remaining, overBudget) = await svc.CheckCreditBudgetAsync(userId);

        Assert.Equal(500_000L, remaining);
        Assert.False(overBudget);
    }

    [Fact]
    public async Task CheckCreditBudget_FreshUser_NotOverBudget()
    {
        var svc = CreateService();

        var (_, overBudget) = await svc.CheckCreditBudgetAsync(Guid.NewGuid());

        Assert.False(overBudget);
    }
}
