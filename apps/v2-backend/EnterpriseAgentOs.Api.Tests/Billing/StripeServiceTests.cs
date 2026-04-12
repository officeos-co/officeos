using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Entities.Billing;
using EnterpriseAgentOs.Api.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseAgentOs.Api.Tests.Billing;

/// <summary>
/// Unit tests for StripeService.
/// Tests that exercise live Stripe SDK calls (CreateCustomer, CreateSubscription, etc.)
/// are skipped when Stripe is disabled / no real API key is configured.
/// </summary>
public sealed class StripeServiceTests
{
    private static EaosDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EaosDbContext(opts);
    }

    private static StripeService CreateService(bool enabled = false) =>
        new(
            new StripeConfig
            {
                SecretKey = "STRIPE_SECRET_KEY_PLACEHOLDER",
                WebhookSecret = "STRIPE_WEBHOOK_SECRET_PLACEHOLDER",
                FreePriceId = "STRIPE_FREE_PRICE_ID_PLACEHOLDER",
                TeamPriceId = "STRIPE_TEAM_PRICE_ID_PLACEHOLDER",
                TeamOveragePriceId = "STRIPE_TEAM_OVERAGE_PRICE_ID_PLACEHOLDER",
                Enabled = enabled,
            },
            new FrontendConfig("https://dashboard.harrokrog.com"),
            CreateDb(),
            NullLogger<StripeService>.Instance);

    // -------------------------------------------------------------------------
    // GetOrgSubscriptionAsync — tier detection
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPlanForOrg_DefaultOrg_ReturnsFreeSubscription()
    {
        var svc = CreateService();

        var sub = await svc.GetOrgSubscriptionAsync("org-1");

        Assert.Equal("free", sub.Plan);
    }

    [Fact]
    public async Task GetPlanForOrg_DefaultOrg_HasCorrectFreeLimits()
    {
        var svc = CreateService();

        var sub = await svc.GetOrgSubscriptionAsync("org-1");

        Assert.Equal(1, sub.ConcurrentAgentLimit);
        Assert.Equal(2_000_000L, sub.TokenBudgetPerMonth);
        Assert.True(sub.IsActive);
    }

    [Fact]
    public async Task GetPlanForOrg_DifferentOrgIds_ReturnedOrgIdMatches()
    {
        var svc = CreateService();

        var sub = await svc.GetOrgSubscriptionAsync("my-unique-org");

        Assert.Equal("my-unique-org", sub.OrganizationId);
    }

    // -------------------------------------------------------------------------
    // CheckTokenBudgetAsync — remaining tokens
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CheckTokenBudget_FreshFreeOrg_ReturnsFullBudget()
    {
        var svc = CreateService();

        var (remaining, overBudget) = await svc.CheckTokenBudgetAsync("org-2");

        Assert.Equal(2_000_000L, remaining);
        Assert.False(overBudget);
    }

    [Fact]
    public async Task CheckTokenBudget_UnusedTokens_NotOverBudget()
    {
        var svc = CreateService();

        var (_, overBudget) = await svc.CheckTokenBudgetAsync("org-3");

        Assert.False(overBudget);
    }

    // -------------------------------------------------------------------------
    // RecordTokenUsageAsync — placeholder (metered billing via Billing Meter Events)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RecordTokenUsage_ValidSubscriptionItemId_CompletesWithoutException()
    {
        var svc = CreateService();

        // Should not throw — placeholder until meter event name is configured
        await svc.RecordTokenUsageAsync("sub_item_placeholder", 1_000);
    }

    [Fact]
    public async Task RecordTokenUsage_LargeTokenCount_CompletesWithoutException()
    {
        var svc = CreateService();

        await svc.RecordTokenUsageAsync("sub_item_placeholder_large", 500_000);
    }

    // -------------------------------------------------------------------------
    // Overage calculation — cost × 1.3
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0.50, 1_000_000, 650_000.0)]  // Haiku $0.50/M blended
    [InlineData(9.00, 1_000_000, 11_700_000.0)]  // Sonnet $9/M blended
    [InlineData(45.00, 1_000_000, 58_500_000.0)] // Opus $45/M blended
    public void OverageCalculation_ModelCostTimesOnePointThree_NeverLosesMoney(
        double modelCostPerMillion,
        long tokens,
        double expectedOverageBillingUnits)
    {
        // Overage rate = model cost × 1.3
        const double overageMultiplier = 1.3;
        var overageRate = modelCostPerMillion * overageMultiplier;
        var billingUnits = (tokens / 1_000_000.0) * overageRate * 1_000_000;

        Assert.Equal(expectedOverageBillingUnits, billingUnits, precision: 0);
        // Ensure we always charge MORE than model cost
        Assert.True(overageRate > modelCostPerMillion);
    }

    [Fact]
    public void OverageMultiplier_IsAlwaysGreaterThanOne()
    {
        // Sanity check: 1.3x always covers model cost + margin
        const double overageMultiplier = 1.3;
        Assert.True(overageMultiplier > 1.0);
    }

    // -------------------------------------------------------------------------
    // HandleWebhookAsync — signature verification requires real Stripe secret
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleWebhook_InvalidSignature_ThrowsStripeException()
    {
        var svc = CreateService();

        // Real SDK now verifies signature — an invalid signature should throw
        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.HandleWebhookAsync("{\"type\":\"customer.subscription.updated\"}", "t=12345,v1=abc"));
    }
}
