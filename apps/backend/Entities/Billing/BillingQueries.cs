namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class BillingQueries
{
    public async Task<EnterpriseAgentOs.Api.Entities.Billing.Types.UserSubscriptionDto> GetUserSubscription(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new EnterpriseAgentOs.Api.Entities.Billing.Types.UserSubscriptionDto(
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

    public async Task<EnterpriseAgentOs.Api.Entities.Billing.Types.OrgSubscriptionDto> GetOrgSubscription(
        string organizationId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Billing.IOrgBillingService orgBilling,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await orgBilling.GetSubscriptionAsync(organizationId, ct);
        var (remaining, overBudget) = await orgBilling.CheckCreditBudgetAsync(organizationId, ct);
        return new EnterpriseAgentOs.Api.Entities.Billing.Types.OrgSubscriptionDto(
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

    public EnterpriseAgentOs.Api.Entities.Billing.Types.PlanLimitsDto GetPlanLimits(IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return new EnterpriseAgentOs.Api.Entities.Billing.Types.PlanLimitsDto(
            ToDto(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualFree),
            ToDto(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualPro),
            ToDto(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.OrgFree),
            ToDto(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.OrgTeam));
    }

    public IReadOnlyList<EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto> GetModelCostWeights(IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        // Mirrors the internal Weights dict in ModelCostWeights — dashboard-facing.
        return new[]
        {
            new EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto("gpt-4o-mini", 1),
            new EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto("gemini-2.5-flash", 1),
            new EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto("claude-haiku-4-5", 5),
            new EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto("gemini-2.5-pro", 8),
            new EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto("gpt-4o", 15),
            new EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto("claude-sonnet-4-6", 20),
            new EnterpriseAgentOs.Api.Entities.Billing.Types.ModelCostWeightDto("claude-opus-4-6", 75),
        };
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Billing.Types.UsageSummaryDto> GetTokenUsage(
        string? range,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        // range reserved for future aggregations (e.g. "7d", "30d"); current service
        // only exposes month-to-date against the subscription row.
        _ = range;
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, _) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new EnterpriseAgentOs.Api.Entities.Billing.Types.UsageSummaryDto(
            sub.CreditsUsedThisMonth,
            sub.CreditBudgetPerMonth,
            remaining,
            sub.PeriodStart,
            sub.PeriodEnd);
    }

    private static EnterpriseAgentOs.Api.Entities.Billing.Types.PlanLimitDto ToDto(EnterpriseAgentOs.Api.Entities.Billing.PlanLimit p) =>
        new(p.Plan, p.ConcurrentAgents, p.CreditsPerMonth);
}
