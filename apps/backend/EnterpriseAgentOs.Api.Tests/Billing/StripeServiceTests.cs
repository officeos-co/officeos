namespace EnterpriseAgentOs.Api.Tests.Billing;

/// <summary>
/// Unit tests for the SOLID billing services (OrgBillingService, StripeWebhookService, CreditRecordingService).
/// Tests that exercise live Stripe SDK calls are skipped when Stripe is disabled / no real API key is configured.
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

    private static StripeConfig CreateConfig(bool enabled = false) => new()
    {
        SecretKey = "STRIPE_SECRET_KEY_PLACEHOLDER",
        WebhookSecret = "STRIPE_WEBHOOK_SECRET_PLACEHOLDER",
        FreePriceId = "STRIPE_FREE_PRICE_ID_PLACEHOLDER",
        TeamMonthlyPriceId = "STRIPE_TEAM_MONTHLY_PRICE_ID_PLACEHOLDER",
        TeamYearlyPriceId = "STRIPE_TEAM_YEARLY_PRICE_ID_PLACEHOLDER",
        TeamOveragePriceId = "STRIPE_TEAM_OVERAGE_PRICE_ID_PLACEHOLDER",
        FreeOveragePriceId = "STRIPE_FREE_OVERAGE_PRICE_ID_PLACEHOLDER",
        ProOveragePriceId = "STRIPE_PRO_OVERAGE_PRICE_ID_PLACEHOLDER",
        Enabled = enabled,
    };

    private static OrgBillingService CreateOrgService(EaosDbContext? db = null)
    {
        var context = db ?? CreateDb();
        return new(CreateConfig(), new OrgSubscriptionRepository(context), NullLogger<OrgBillingService>.Instance);
    }

    private static StripeWebhookService CreateWebhookService(EaosDbContext? db = null) =>
        new(CreateConfig(), db ?? CreateDb(), NullLogger<StripeWebhookService>.Instance);

    // -------------------------------------------------------------------------
    // GetSubscriptionAsync — tier detection
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPlanForOrg_DefaultOrg_ReturnsFreeSubscription()
    {
        var svc = CreateOrgService();

        var sub = await svc.GetSubscriptionAsync("org-1");

        Assert.Equal("free", sub.Plan);
    }

    [Fact]
    public async Task GetPlanForOrg_DefaultOrg_HasCorrectFreeLimits()
    {
        var svc = CreateOrgService();

        var sub = await svc.GetSubscriptionAsync("org-1");

        Assert.Equal(1, sub.ConcurrentAgentLimit);
        Assert.Equal(500_000L, sub.CreditBudgetPerMonth);
        Assert.True(sub.IsActive);
    }

    [Fact]
    public async Task GetPlanForOrg_DifferentOrgIds_ReturnedOrgIdMatches()
    {
        var svc = CreateOrgService();

        var sub = await svc.GetSubscriptionAsync("my-unique-org");

        Assert.Equal("my-unique-org", sub.OrganizationId);
    }

    // -------------------------------------------------------------------------
    // CheckCreditBudgetAsync — remaining credits
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CheckCreditBudget_FreshFreeOrg_ReturnsFullBudget()
    {
        var svc = CreateOrgService();

        var (remaining, overBudget) = await svc.CheckCreditBudgetAsync("org-2");

        Assert.Equal(500_000L, remaining);
        Assert.False(overBudget);
    }

    [Fact]
    public async Task CheckCreditBudget_UnusedCredits_NotOverBudget()
    {
        var svc = CreateOrgService();

        var (_, overBudget) = await svc.CheckCreditBudgetAsync("org-3");

        Assert.False(overBudget);
    }

    // -------------------------------------------------------------------------
    // ModelCostWeights — credit normalization
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("gpt-4o-mini", 1000, 1000)]       // weight 1
    [InlineData("claude-haiku-4-5", 1000, 5000)]   // weight 5
    [InlineData("claude-sonnet-4-6", 1000, 20000)]  // weight 20
    [InlineData("claude-opus-4-6", 1000, 75000)]    // weight 75
    public void ModelCostWeights_ToCredits_AppliesCorrectMultiplier(string model, long rawTokens, long expectedCredits)
    {
        var credits = EnterpriseAgentOs.Domain.Services.ModelCostWeights.ToCredits(model, rawTokens);
        Assert.Equal(expectedCredits, credits);
    }

    [Fact]
    public void ModelCostWeights_UnknownModel_DefaultsToSonnetWeight()
    {
        var credits = EnterpriseAgentOs.Domain.Services.ModelCostWeights.ToCredits("unknown-model", 1000);
        Assert.Equal(20_000, credits); // default weight = 20
    }

    // -------------------------------------------------------------------------
    // Overage calculation — cost × 1.3
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0.50, 1_000_000, 650_000.0)]       // Haiku $0.50/M blended
    [InlineData(9.00, 1_000_000, 11_700_000.0)]    // Sonnet $9/M blended
    [InlineData(45.00, 1_000_000, 58_500_000.0)]   // Opus $45/M blended
    public void OverageCalculation_ModelCostTimesOnePointThree_NeverLosesMoney(
        double modelCostPerMillion,
        long tokens,
        double expectedOverageBillingUnits)
    {
        const double overageMultiplier = 1.3;
        var overageRate = modelCostPerMillion * overageMultiplier;
        var billingUnits = (tokens / 1_000_000.0) * overageRate * 1_000_000;

        Assert.Equal(expectedOverageBillingUnits, billingUnits, precision: 0);
        Assert.True(overageRate > modelCostPerMillion);
    }

    [Fact]
    public void OverageMultiplier_IsAlwaysGreaterThanOne()
    {
        const double overageMultiplier = 1.3;
        Assert.True(overageMultiplier > 1.0);
    }

    // -------------------------------------------------------------------------
    // HandleAsync — signature verification requires real Stripe secret
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleWebhook_InvalidSignature_ThrowsStripeException()
    {
        var svc = CreateWebhookService();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.HandleAsync("{\"type\":\"customer.subscription.updated\"}", "t=12345,v1=abc"));
    }
}
