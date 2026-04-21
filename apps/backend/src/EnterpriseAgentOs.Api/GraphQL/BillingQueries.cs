namespace EnterpriseAgentOs.Api.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class BillingQueries
{
    private static readonly TimeSpan BillingCacheTtl = TimeSpan.FromMinutes(1);

    public async Task<UserSubscriptionDto> GetUserSubscription(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"billing:sub:{user.Id}";

        if (cache.TryGetValue(cacheKey, out UserSubscriptionDto? cached) && cached is not null)
            return cached;

        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        var result = new UserSubscriptionDto(
            sub.Id,
            sub.UserId,
            sub.Plan,
            sub.BillingCycle,
            sub.ConcurrentAgentLimit,
            sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth,
            remaining,
            overBudget,
            sub.OverageEnabled,
            sub.PeriodStart,
            sub.PeriodEnd,
            sub.IsActive);

        cache.Set(cacheKey, result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = BillingCacheTtl });
        return result;
    }

    public async Task<OrgSubscriptionDto> GetOrgSubscription(
        string organizationId,
        IResolverContext context,
        [Service] IOrgBillingService orgBilling,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await orgBilling.GetSubscriptionAsync(organizationId, ct);
        var (remaining, overBudget) = await orgBilling.CheckCreditBudgetAsync(organizationId, ct);
        return new OrgSubscriptionDto(
            sub.Id,
            sub.OrganizationId,
            sub.Plan,
            sub.ConcurrentAgentLimit,
            sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth,
            remaining,
            overBudget,
            sub.OverageEnabled,
            sub.PeriodStart,
            sub.PeriodEnd,
            sub.IsActive);
    }

    public PlanLimitsDto GetPlanLimits(IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return new PlanLimitsDto(
            ToDto(PlanLimits.IndividualFree),
            ToDto(PlanLimits.IndividualPro),
            ToDto(PlanLimits.OrgFree),
            ToDto(PlanLimits.OrgTeam));
    }

    public IReadOnlyList<ModelCostWeightDto> GetModelCostWeights(IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return ModelCostWeights.GetWeights()
            .Select(kv => new ModelCostWeightDto(kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>
    /// Unified billing info for the dashboard /billing page. Plan, current
    /// usage, extra-usage on/off (replaces old auto-reload), invoices synced
    /// from Stripe.
    /// </summary>
    public async Task<BillingPayload> Billing(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"billing:dashboard:{user.Id}";

        if (cache.TryGetValue(cacheKey, out BillingPayload? cached) && cached is not null)
            return cached;

        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        var invoices = await userBilling.ListInvoicesAsync(user.Id, ct);
        var planDescription = PlanLimits.ForIndividualPlan(sub.Plan).Description;
        var result = new BillingPayload(
            sub.Plan,
            planDescription,
            sub.IsActive ? "active" : "canceled",
            sub.BillingCycle,
            sub.PeriodStart,
            sub.PeriodEnd,
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

    public async Task<UsageSummaryDto> GetTokenUsage(
        string? range,
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        CancellationToken ct)
    {
        // range reserved for future aggregations (e.g. "7d", "30d"); current service
        // only exposes month-to-date against the subscription row.
        _ = range;
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, _) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new UsageSummaryDto(
            sub.CreditsUsedThisMonth,
            sub.CreditBudgetPerMonth,
            remaining,
            sub.PeriodStart,
            sub.PeriodEnd);
    }

    private static PlanLimitDto ToDto(PlanLimit p) =>
        new(p.Plan, p.ConcurrentAgents, p.CreditsPerMonth);
}
