namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class BillingQueries
{
    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.UserSubscriptionDto> GetUserSubscription(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new EnterpriseAgentOs.Api.GraphQL.Types.UserSubscriptionDto(
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

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.OrgSubscriptionDto> GetOrgSubscription(
        string organizationId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Billing.IOrgBillingService orgBilling,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await orgBilling.GetSubscriptionAsync(organizationId, ct);
        var (remaining, overBudget) = await orgBilling.CheckCreditBudgetAsync(organizationId, ct);
        return new EnterpriseAgentOs.Api.GraphQL.Types.OrgSubscriptionDto(
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

    public EnterpriseAgentOs.Api.GraphQL.Types.PlanLimitsDto GetPlanLimits(IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return new EnterpriseAgentOs.Api.GraphQL.Types.PlanLimitsDto(
            ToDto(EnterpriseAgentOs.Domain.DTOs.Billing.PlanLimits.IndividualFree),
            ToDto(EnterpriseAgentOs.Domain.DTOs.Billing.PlanLimits.IndividualPro),
            ToDto(EnterpriseAgentOs.Domain.DTOs.Billing.PlanLimits.OrgFree),
            ToDto(EnterpriseAgentOs.Domain.DTOs.Billing.PlanLimits.OrgTeam));
    }

    public IReadOnlyList<EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto> GetModelCostWeights(IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        // Mirrors the internal Weights dict in ModelCostWeights — dashboard-facing.
        return new[]
        {
            new EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto("gpt-4o-mini", 1),
            new EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto("gemini-2.5-flash", 1),
            new EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto("claude-haiku-4-5", 5),
            new EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto("gemini-2.5-pro", 8),
            new EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto("gpt-4o", 15),
            new EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto("claude-sonnet-4-6", 20),
            new EnterpriseAgentOs.Api.GraphQL.Types.ModelCostWeightDto("claude-opus-4-6", 75),
        };
    }

    /// <summary>
    /// Unified billing info for the dashboard /billing page. Plan, current
    /// usage, extra-usage on/off (replaces old auto-reload), invoices synced
    /// from Stripe.
    /// </summary>
    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.BillingPayload> Billing(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        var invoices = await userBilling.ListInvoicesAsync(user.Id, ct);
        var planDescription = sub.Plan == "pro"
            ? "3 concurrent agents, 10M credits/month"
            : "1 concurrent agent, 500k credits/month";
        return new EnterpriseAgentOs.Api.GraphQL.Types.BillingPayload(
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

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.UsageSummaryDto> GetTokenUsage(
        string? range,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        // range reserved for future aggregations (e.g. "7d", "30d"); current service
        // only exposes month-to-date against the subscription row.
        _ = range;
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, _) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new EnterpriseAgentOs.Api.GraphQL.Types.UsageSummaryDto(
            sub.CreditsUsedThisMonth,
            sub.CreditBudgetPerMonth,
            remaining,
            sub.PeriodStart,
            sub.PeriodEnd);
    }

    private static EnterpriseAgentOs.Api.GraphQL.Types.PlanLimitDto ToDto(EnterpriseAgentOs.Domain.DTOs.Billing.PlanLimit p) =>
        new(p.Plan, p.ConcurrentAgents, p.CreditsPerMonth);
}
