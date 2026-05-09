namespace EnterpriseAgentOs.Application.Features.Analytics;

internal sealed class UsageAnalyticsService : IUsageAnalyticsService
{
    private readonly IUserBillingService _userBilling;
    private readonly IAgentLogRepository _agentLogRepository;
    private readonly IHostEnvironment _env;

    public UsageAnalyticsService(
        IUserBillingService userBilling,
        IAgentLogRepository agentLogRepository,
        IHostEnvironment env)
    {
        _userBilling = userBilling;
        _agentLogRepository = agentLogRepository;
        _env = env;
    }

    public async Task<UsageAnalyticsDto> GetForUserAsync(Guid userId, UsageAnalyticsInput input, CancellationToken ct = default)
    {
        var (from, toExclusive) = NormalizeRange(input.From, input.To);
        var sub = await _userBilling.GetSubscriptionAsync(userId, ct);
        var rows = await _agentLogRepository.ListUsageAggregatesAsync(userId, from, toExclusive, ct);

        var points = BuildEmptyPoints(from, toExclusive);
        long totalTokens = 0;
        long totalCredits = 0;

        foreach (var row in rows)
        {
            var tokens = row.InputTokens + row.OutputTokens;
            if (tokens <= 0) continue;

            var credits = ProviderRegistry.ToCredits(row.Model, tokens);
            totalTokens += tokens;
            totalCredits += credits;

            var key = row.Date.Date;
            if (points.TryGetValue(key, out var existing))
            {
                points[key] = existing with
                {
                    Tokens = existing.Tokens + tokens,
                    Credits = existing.Credits + credits,
                };
            }
        }

        return new UsageAnalyticsDto(
            from,
            toExclusive,
            totalTokens,
            totalCredits,
            CalculateCost(sub, totalCredits, from, toExclusive),
            points.Values.OrderBy(p => p.Date).ToList());
    }

    private static Dictionary<DateTime, UsageAnalyticsPointDto> BuildEmptyPoints(DateTime from, DateTime toExclusive)
    {
        return Enumerable.Range(0, (int)Math.Ceiling((toExclusive - from).TotalDays))
            .Select(offset =>
            {
                var date = from.Date.AddDays(offset);
                return new UsageAnalyticsPointDto(date, 0, 0);
            })
            .ToDictionary(p => p.Date.Date);
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

    private UsageCostBreakdownDto CalculateCost(
        UserSubscription sub,
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
        return new UsageCostBreakdownDto(
            included + onDemand,
            included,
            onDemand,
            "USD",
            _env.IsDevelopment());
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
