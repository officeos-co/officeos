namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class BillingQueries
{
    private static readonly TimeSpan BillingCacheTtl = TimeSpan.FromMinutes(1);

    [GraphQLDescription("Returns the authenticated user's billing subscription including plan, credits, limits, and period.")]
    public async Task<UserSubscriptionDto> GetUserSubscription(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"billing:sub:{user.Id}";

        var cached = await cache.GetJsonAsync<UserSubscriptionDto>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        var result = new UserSubscriptionDto(
            sub.Id,
            sub.UserId,
            sub.Plan.ToStorageString(),
            sub.BillingCycle.ToStorageString(),
            sub.ConcurrentAgentLimit,
            sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth,
            remaining,
            overBudget,
            sub.OverageEnabled,
            sub.Period.Start,
            sub.Period.End,
            sub.IsActive);

        await cache.SetJsonAsync(cacheKey, result, BillingCacheTtl, ct);
        return result;
    }

    [GraphQLDescription("Returns billing subscription for a specific organization.")]
    public async Task<OrgSubscriptionDto> GetOrgSubscription(
        string organizationId,
        IResolverContext context,
        [Service] IOrgBillingService orgBilling,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var sub = await orgBilling.GetSubscriptionAsync(organizationId, ct);
        var (remaining, overBudget) = await orgBilling.CheckCreditBudgetAsync(organizationId, ct);
        return new OrgSubscriptionDto(
            sub.Id,
            sub.OrganizationId,
            sub.Plan.ToStorageString(),
            sub.ConcurrentAgentLimit,
            sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth,
            remaining,
            overBudget,
            sub.OverageEnabled,
            sub.Period.Start,
            sub.Period.End,
            sub.IsActive);
    }

    [GraphQLDescription("Returns the limits for all plan tiers (free, pro, org-free, org-team) including concurrent agents and credits per month.")]
    public PlanLimitsDto GetPlanLimits(IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return new PlanLimitsDto(
            PlanLimits.IndividualFree,
            PlanLimits.IndividualPro,
            PlanLimits.OrgFree,
            PlanLimits.OrgTeam);
    }

    [GraphQLDescription("Returns the credit cost weight multiplier for each supported LLM model.")]
    public IReadOnlyList<ModelCostWeightDto> GetModelCostWeights(
        IResolverContext context,
        [Service] CustomLlmProviderConfig customLlmProviderConfig)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var weights = ProviderRegistry.GetCostWeights()
            .Select(kv => new ModelCostWeightDto(kv.Key, kv.Value))
            .ToList();

        if (customLlmProviderConfig.IsConfigured)
        {
            weights.Add(new ModelCostWeightDto(
                customLlmProviderConfig.ModelId.Trim(),
                customLlmProviderConfig.EffectiveCostWeight));
        }

        return weights;
    }

    /// <summary>
    /// Unified billing info for the dashboard /billing page.
    /// </summary>
    [GraphQLDescription("Unified billing info for the dashboard billing page. Includes plan, usage, payment method, and invoice history.")]
    public async Task<BillingPayload> Billing(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"billing:dashboard:{user.Id}";

        var cached = await cache.GetJsonAsync<BillingPayload>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        var invoices = await userBilling.ListInvoicesAsync(user.Id, ct);
        var planDescription = PlanLimits.ForIndividualPlan(sub.Plan).Description;
        var result = new BillingPayload(
            sub.Plan.ToStorageString(),
            planDescription,
            sub.IsActive ? "active" : "canceled",
            sub.BillingCycle.ToStorageString(),
            sub.Period.Start,
            sub.Period.End,
            sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth,
            remaining,
            overBudget,
            sub.OverageEnabled,
            null,
            null,
            invoices);

        await cache.SetJsonAsync(cacheKey, result, BillingCacheTtl, ct);
        return result;
    }

    [GraphQLDescription("Returns the price of each plan tier (monthly and yearly) as configured in Stripe.")]
    public async Task<IReadOnlyList<PlanPriceDto>> GetPlanPrices(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        const string cacheKey = "billing:plan-prices";

        var cached = await cache.GetJsonAsync<IReadOnlyList<PlanPriceDto>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var prices = await userBilling.GetPlanPricesAsync(ct);
        var result = prices
            .Select(kv => new PlanPriceDto(kv.Key, kv.Value.MonthlyAmountCents, kv.Value.YearlyAmountCents, kv.Value.Currency))
            .ToList();

        await cache.SetJsonAsync(cacheKey, (IReadOnlyList<PlanPriceDto>)result, TimeSpan.FromMinutes(10), ct);
        return result;
    }

    [GraphQLDescription("Returns credit and token usage for the current billing period. Optional range param for historical periods.")]
    public async Task<UserSubscriptionDto> GetTokenUsage(
        string? range,
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        CancellationToken ct)
    {
        _ = range;
        var user = DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new UserSubscriptionDto(
            sub.Id,
            sub.UserId,
            sub.Plan.ToStorageString(),
            sub.BillingCycle.ToStorageString(),
            sub.ConcurrentAgentLimit,
            sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth,
            remaining,
            overBudget,
            sub.OverageEnabled,
            sub.Period.Start,
            sub.Period.End,
            sub.IsActive);
    }

    [GraphQLDescription("Returns token usage over time plus backend-calculated spend for an exact date range.")]
    public async Task<UsageAnalyticsDto> GetUsageAnalytics(
        UsageAnalyticsInput input,
        IResolverContext context,
        [Service] IUsageAnalyticsService usageAnalytics,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await usageAnalytics.GetForUserAsync(user.Id, input, ct);
    }
}
