namespace EnterpriseAgentOs.Domain.Models;

/// <summary>
/// Single source of truth for plan limits. Used by both billing enforcement
/// and the API response so the frontend never hardcodes limits.
/// </summary>
public static class PlanLimits
{
    // ── Individual plans ─────────────────────────────────────────────────────
    public static readonly PlanLimit IndividualFree = new("free",  1,    500_000L);
    public static readonly PlanLimit IndividualPro  = new("pro",   3, 10_000_000L);

    // ── Org plans ─────────────────────────────────────────────────────────────
    public static readonly PlanLimit OrgFree  = new("free",  1,    500_000L);
    public static readonly PlanLimit OrgTeam  = new("team", 10, 25_000_000L);
    // Enterprise limits are custom — stored on OrgSubscription directly.

    public static PlanLimit ForIndividualPlan(string plan) => plan switch
    {
        "pro" => IndividualPro,
        _     => IndividualFree,   // default to free
    };

    public static PlanLimit ForOrgPlan(string plan) => plan switch
    {
        "team"       => OrgTeam,
        "enterprise" => throw new ArgumentException("Enterprise limits are stored on OrgSubscription", nameof(plan)),
        _            => OrgFree,
    };
}

public sealed record PlanLimit(string Plan, int ConcurrentAgents, long CreditsPerMonth)
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
