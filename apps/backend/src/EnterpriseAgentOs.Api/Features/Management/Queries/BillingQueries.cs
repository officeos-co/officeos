namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class BillingQueries
{
    private static readonly TimeSpan BillingCacheTtl = TimeSpan.FromMinutes(1);

    [GraphQLDescription("Returns the authenticated user's billing subscription including plan, credits, limits, and period.")]
    public async Task<UserSubscriptionDto> GetUserSubscription(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"billing:sub:{user.Id}";

        if (cache.TryGetValue(cacheKey, out UserSubscriptionDto? cached) && cached is not null)
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

        cache.Set(cacheKey, result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = BillingCacheTtl });
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
    public IReadOnlyList<ModelCostWeightDto> GetModelCostWeights(IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return ModelCostWeights.GetWeights()
            .Select(kv => new ModelCostWeightDto(kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>
    /// Unified billing info for the dashboard /billing page.
    /// </summary>
    [GraphQLDescription("Unified billing info for the dashboard billing page. Includes plan, usage, payment method, and invoice history.")]
    public async Task<BillingPayload> Billing(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"billing:dashboard:{user.Id}";

        if (cache.TryGetValue(cacheKey, out BillingPayload? cached) && cached is not null)
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

        cache.Set(cacheKey, result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = BillingCacheTtl });
        return result;
    }

    [GraphQLDescription("Returns the price of each plan tier (monthly and yearly) as configured in Stripe.")]
    public async Task<IReadOnlyList<PlanPriceDto>> GetPlanPrices(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        const string cacheKey = "billing:plan-prices";

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<PlanPriceDto>? cached) && cached is not null)
            return cached;

        var prices = await userBilling.GetPlanPricesAsync(ct);
        var result = prices
            .Select(kv => new PlanPriceDto(kv.Key, kv.Value.MonthlyAmountCents, kv.Value.YearlyAmountCents, kv.Value.Currency))
            .ToList();

        cache.Set(cacheKey, (IReadOnlyList<PlanPriceDto>)result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
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
}
