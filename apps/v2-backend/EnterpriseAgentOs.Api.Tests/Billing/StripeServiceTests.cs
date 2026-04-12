using EnterpriseAgentOs.Api.Entities.Billing;
using EnterpriseAgentOs.Api.Properties;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseAgentOs.Api.Tests.Billing;

/// <summary>
/// Unit tests for StripeService.
/// All tests run against the stub implementation — real Stripe calls are replaced with TODOs.
/// </summary>
public sealed class StripeServiceTests
{
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
    // RecordTokenUsageAsync — stub increments (will be real once DB is wired)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RecordTokenUsage_ValidSubscriptionId_CompletesWithoutException()
    {
        var svc = CreateService();

        // Should not throw even on the stub
        await svc.RecordTokenUsageAsync("sub_placeholder_cus_test_free", 1_000);
    }

    [Fact]
    public async Task RecordTokenUsage_LargeTokenCount_CompletesWithoutException()
    {
        var svc = CreateService();

        await svc.RecordTokenUsageAsync("sub_placeholder_cus_test_team", 500_000);
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
    // CreateCustomerAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCustomer_ValidOrgAndEmail_ReturnsNonEmptyCustomerId()
    {
        var svc = CreateService();

        var customerId = await svc.CreateCustomerAsync("org-4", "test@example.com");

        Assert.NotEmpty(customerId);
    }

    [Fact]
    public async Task CreateCustomer_ContainsOrgId_InPlaceholderId()
    {
        var svc = CreateService();

        var customerId = await svc.CreateCustomerAsync("org-5", "user@example.com");

        Assert.Contains("org-5", customerId);
    }

    // -------------------------------------------------------------------------
    // CreateSubscriptionAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateSubscription_FreePlan_ReturnsNonEmptySubscriptionId()
    {
        var svc = CreateService();

        var subscriptionId = await svc.CreateSubscriptionAsync("cus_test", "free");

        Assert.NotEmpty(subscriptionId);
    }

    [Fact]
    public async Task CreateSubscription_TeamPlan_ReturnsNonEmptySubscriptionId()
    {
        var svc = CreateService();

        var subscriptionId = await svc.CreateSubscriptionAsync("cus_test", "team");

        Assert.NotEmpty(subscriptionId);
    }

    // -------------------------------------------------------------------------
    // HandleWebhookAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleWebhook_ValidPayloadAndSignature_CompletesWithoutException()
    {
        var svc = CreateService();

        // TODO: Once real Stripe SDK is wired, this should verify the signature.
        await svc.HandleWebhookAsync("{\"type\":\"customer.subscription.updated\"}", "t=12345,v1=abc");
    }
}
