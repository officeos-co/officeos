namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class BillingQueries
{
    public async Task<Types.UserSubscriptionDto> GetUserSubscription(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new Types.UserSubscriptionDto(
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
    }

    public async Task<Types.OrgSubscriptionDto> GetOrgSubscription(
        string organizationId,
        IResolverContext context,
        [Service] IOrgBillingService orgBilling,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await orgBilling.GetSubscriptionAsync(organizationId, ct);
        var (remaining, overBudget) = await orgBilling.CheckCreditBudgetAsync(organizationId, ct);
        return new Types.OrgSubscriptionDto(
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

    public Types.PlanLimitsDto GetPlanLimits(IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return new Types.PlanLimitsDto(
            ToDto(PlanLimits.IndividualFree),
            ToDto(PlanLimits.IndividualPro),
            ToDto(PlanLimits.OrgFree),
            ToDto(PlanLimits.OrgTeam));
    }

    public IReadOnlyList<Types.ModelCostWeightDto> GetModelCostWeights(IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        // Mirrors the internal Weights dict in ModelCostWeights — dashboard-facing.
        return new[]
        {
            new Types.ModelCostWeightDto("gpt-4o-mini", 1),
            new Types.ModelCostWeightDto("gemini-2.5-flash", 1),
            new Types.ModelCostWeightDto("claude-haiku-4-5", 5),
            new Types.ModelCostWeightDto("gemini-2.5-pro", 8),
            new Types.ModelCostWeightDto("gpt-4o", 15),
            new Types.ModelCostWeightDto("claude-sonnet-4-6", 20),
            new Types.ModelCostWeightDto("claude-opus-4-6", 75),
        };
    }

    /// <summary>
    /// Unified billing info for the dashboard /billing page. Plan, current
    /// usage, extra-usage on/off (replaces old auto-reload), invoices synced
    /// from Stripe.
    /// </summary>
    public async Task<Types.BillingPayload> Billing(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        var invoices = await userBilling.ListInvoicesAsync(user.Id, ct);
        var planDescription = sub.Plan == "pro"
            ? "3 concurrent agents, 10M credits/month"
            : "1 concurrent agent, 500k credits/month";
        return new Types.BillingPayload(
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
    }

    public async Task<Types.UsageSummaryDto> GetTokenUsage(
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
        return new Types.UsageSummaryDto(
            sub.CreditsUsedThisMonth,
            sub.CreditBudgetPerMonth,
            remaining,
            sub.PeriodStart,
            sub.PeriodEnd);
    }

    private static Types.PlanLimitDto ToDto(PlanLimit p) =>
        new(p.Plan, p.ConcurrentAgents, p.CreditsPerMonth);
}
