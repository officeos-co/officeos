using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class BillingQueries
{
    public async Task<UserSubscriptionDto> GetUserSubscription(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new UserSubscriptionDto(
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
        _ = DashboardAuthContextExtensions.GetUser(context);
        return new PlanLimitsDto(
            ToDto(PlanLimits.IndividualFree),
            ToDto(PlanLimits.IndividualPro),
            ToDto(PlanLimits.OrgFree),
            ToDto(PlanLimits.OrgTeam));
    }

    public IReadOnlyList<ModelCostWeightDto> GetModelCostWeights(IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        // Mirrors the internal Weights dict in ModelCostWeights — dashboard-facing.
        return new[]
        {
            new ModelCostWeightDto("gpt-4o-mini", 1),
            new ModelCostWeightDto("gemini-2.5-flash", 1),
            new ModelCostWeightDto("claude-haiku-4-5", 5),
            new ModelCostWeightDto("gemini-2.5-pro", 8),
            new ModelCostWeightDto("gpt-4o", 15),
            new ModelCostWeightDto("claude-sonnet-4-6", 20),
            new ModelCostWeightDto("claude-opus-4-6", 75),
        };
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
        var user = DashboardAuthContextExtensions.GetUser(context);
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
