namespace EnterpriseAgentOs.Domain.Features.Management;

/// <summary>
/// Single source of truth for plan limits. Used by both billing enforcement
/// and the API response so the frontend never hardcodes limits.
/// </summary>
public static class PlanLimits
{
    // ── Individual plans ─────────────────────────────────────────────────────
    public static readonly PlanLimit IndividualFree = new(SubscriptionPlan.Free,  1,    500_000L);
    public static readonly PlanLimit IndividualPro  = new(SubscriptionPlan.Pro,   3, 10_000_000L);

    // ── Org plans ─────────────────────────────────────────────────────────────
    public static readonly PlanLimit OrgFree  = new(SubscriptionPlan.Free,  1,    500_000L);
    public static readonly PlanLimit OrgTeam  = new(SubscriptionPlan.Team, 10, 25_000_000L);

    public static PlanLimit ForIndividualPlan(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Pro => IndividualPro,
        _                    => IndividualFree,
    };

    public static PlanLimit ForOrgPlan(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Team       => OrgTeam,
        SubscriptionPlan.Enterprise => throw new ArgumentException("Enterprise limits are stored on OrgSubscription", nameof(plan)),
        _                           => OrgFree,
    };

    public static BillingPriceKey ForOrgSubscriptionPrice(SubscriptionPlan plan, BillingCycle cycle) => plan switch
    {
        SubscriptionPlan.Team when cycle == BillingCycle.Yearly => BillingPriceKey.TeamYearly,
        SubscriptionPlan.Team                                  => BillingPriceKey.TeamMonthly,
        SubscriptionPlan.Enterprise                            => throw new ArgumentException("Enterprise billing is negotiated outside Stripe checkout.", nameof(plan)),
        _                                                      => BillingPriceKey.Free,
    };
}

public enum BillingPriceKey
{
    Free,
    TeamMonthly,
    TeamYearly,
}

public sealed record PlanLimit(SubscriptionPlan Plan, int ConcurrentAgents, long CreditsPerMonth)
{
    /// <summary>Human-readable plan description derived from the actual limits.</summary>
    public string Description
    {
        get
        {
            var agentWord = ConcurrentAgents == 1 ? "agent" : "agents";
            var credits = CreditsPerMonth >= 1_000_000
                ? $"{CreditsPerMonth / 1_000_000M:#.##}M"
                : $"{CreditsPerMonth / 1_000M:#.##}k";
            return $"{ConcurrentAgents} concurrent {agentWord}, {credits} credits/month";
        }
    }
}
