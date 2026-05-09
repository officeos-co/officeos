namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class BillingQueries
{
    private static readonly TimeSpan BillingCacheTtl = TimeSpan.FromMinutes(1);

    [GraphQLDescription("Returns the authenticated user's billing subscription including plan, credits, limits, and period.")]
    public async Task<UserSubscriptionPayload> GetUserSubscription(
        [Service] UserContext user,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var cacheKey = $"billing:sub:{user.Id}";

        var cached = await cache.GetJsonAsync<UserSubscriptionPayload>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        var result = new UserSubscriptionPayload(
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
    public async Task<OrgSubscriptionPayload> GetOrgSubscription(
        string organizationId,
        [Service] IOrgBillingService orgBilling,
        CancellationToken ct)
    {
        var sub = await orgBilling.GetSubscriptionAsync(organizationId, ct);
        var (remaining, overBudget) = await orgBilling.CheckCreditBudgetAsync(organizationId, ct);
        return new OrgSubscriptionPayload(
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
    public PlanLimitsPayload GetPlanLimits()
    {
        return new PlanLimitsPayload(
            PlanLimits.IndividualFree,
            PlanLimits.IndividualPro,
            PlanLimits.OrgFree,
            PlanLimits.OrgTeam);
    }

    [GraphQLDescription("Returns the credit cost weight multiplier for each supported LLM model.")]
    public IReadOnlyList<ModelCostWeightPayload> GetModelCostWeights(
        [Service] CustomLlmProviderConfig customLlmProviderConfig)
    {
        var weights = ProviderRegistry.GetCostWeights()
            .Select(kv => new ModelCostWeightPayload(kv.Key, kv.Value))
            .ToList();

        if (customLlmProviderConfig.IsConfigured)
        {
            weights.Add(new ModelCostWeightPayload(
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
        [Service] UserContext user,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
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
    public async Task<IReadOnlyList<PlanPricePayload>> GetPlanPrices(
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        const string cacheKey = "billing:plan-prices";

        var cached = await cache.GetJsonAsync<IReadOnlyList<PlanPricePayload>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var prices = await userBilling.GetPlanPricesAsync(ct);
        var result = prices
            .Select(kv => new PlanPricePayload(kv.Key, kv.Value.MonthlyAmountCents, kv.Value.YearlyAmountCents, kv.Value.Currency))
            .ToList();

        await cache.SetJsonAsync(cacheKey, (IReadOnlyList<PlanPricePayload>)result, TimeSpan.FromMinutes(10), ct);
        return result;
    }

    [GraphQLDescription("Returns credit and token usage for the current billing period. Optional range param for historical periods.")]
    public async Task<UserSubscriptionPayload> GetTokenUsage(
        string? range,
        [Service] UserContext user,
        [Service] IUserBillingService userBilling,
        CancellationToken ct)
    {
        _ = range;
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new UserSubscriptionPayload(
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
    public async Task<UsageAnalyticsResult> GetUsageAnalytics(
        UsageAnalyticsInput input,
        [Service] UserContext user,
        [Service] IUsageAnalyticsService usageAnalytics,
        CancellationToken ct)
    {
        return await usageAnalytics.GetForUserAsync(user.Id, new UsageAnalyticsRequest(input.From, input.To), ct);
    }
}
