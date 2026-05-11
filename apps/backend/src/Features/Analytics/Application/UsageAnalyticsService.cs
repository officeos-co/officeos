namespace OffceOs.Application.Features.Analytics;

internal sealed class UsageAnalyticsService : IUsageAnalyticsService
{
    private readonly IUserBillingService _userBillingService;
    private readonly IAgentUsageAnalyticsService _agentUsageAnalyticsService;
    private readonly IHostEnvironment _hostEnvironment;

    public UsageAnalyticsService(
        IUserBillingService userBillingService,
        IAgentUsageAnalyticsService agentUsageAnalyticsService,
        IHostEnvironment hostEnvironment)
    {
        _userBillingService = userBillingService;
        _agentUsageAnalyticsService = agentUsageAnalyticsService;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<UsageAnalyticsResult> GetForUserAsync(Guid userId, UsageAnalyticsRequest input, CancellationToken ct = default)
    {
        var (from, toExclusive) = NormalizeRange(input.From, input.To);
        var sub = await _userBillingService.GetSubscriptionAsync(userId, ct);
        var dashboard = await _agentUsageAnalyticsService.GetDashboardAsync(
            userId,
            new AgentUsageAnalyticsRequest(from, toExclusive.AddDays(-1)),
            ct);

        return new UsageAnalyticsResult(
            from,
            toExclusive,
            dashboard.TotalTokens,
            dashboard.TotalCredits,
            CalculateCost(sub, dashboard.TotalCredits, from, toExclusive),
            dashboard.Daily.Select(p => new UsageAnalyticsPoint(p.Date, p.Tokens, p.Credits)).ToList());
    }

    private static (DateTime From, DateTime ToExclusive) NormalizeRange(DateTime from, DateTime to)
    {
        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc).AddDays(1);
        if (end <= start)
            end = start.AddDays(1);

        var maxEnd = start.AddDays(366);
        if (end > maxEnd)
            end = maxEnd;

        return (start, end);
    }

    private UsageCostBreakdown CalculateCost(
        UserSubscriptionRecord sub,
        long rangeCredits,
        DateTime from,
        DateTime toExclusive)
    {
        var selectedDays = Math.Max(1m, (decimal)(toExclusive - from).TotalDays);
        var periodDays = Math.Max(1m, (decimal)(sub.Period.End - sub.Period.Start).TotalDays);
        var overlapStart = from > sub.Period.Start ? from : sub.Period.Start;
        var overlapEnd = toExclusive < sub.Period.End ? toExclusive : sub.Period.End;
        var overlapDays = overlapEnd > overlapStart
            ? Math.Max(1m, (decimal)(overlapEnd - overlapStart).TotalDays)
            : selectedDays;

        var includedCreditsForRange = sub.CreditBudgetPerMonth * (overlapDays / periodDays);
        var onDemandCredits = Math.Max(0m, rangeCredits - includedCreditsForRange);
        var includedCents = PlanMonthlyCents(sub.Plan, sub.BillingCycle) * (overlapDays / periodDays);
        var onDemandCents = onDemandCredits / 1_000_000m * OverageCentsPerMillion(sub.Plan);

        var included = RoundCents(includedCents);
        var onDemand = RoundCents(onDemandCents);
        return new UsageCostBreakdown(
            included + onDemand,
            included,
            onDemand,
            "USD",
            _hostEnvironment.IsDevelopment());
    }

    private static decimal PlanMonthlyCents(SubscriptionPlan plan, BillingCycle cycle) =>
        plan switch
        {
            SubscriptionPlan.Pro when cycle == BillingCycle.Yearly => 1_600m,
            SubscriptionPlan.Pro => 2_000m,
            _ => 0m,
        };

    private static decimal OverageCentsPerMillion(SubscriptionPlan plan) =>
        plan == SubscriptionPlan.Pro ? 300m : 500m;

    private static long RoundCents(decimal cents) =>
        (long)Math.Round(cents, MidpointRounding.AwayFromZero);
}
