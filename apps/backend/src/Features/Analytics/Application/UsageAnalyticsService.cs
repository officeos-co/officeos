namespace OffceOs.Application.Features.Analytics;

internal sealed class UsageAnalyticsService : IUsageAnalyticsService
{
    private readonly IAgentUsageAnalyticsService _agentUsageAnalyticsService;

    public UsageAnalyticsService(
        IAgentUsageAnalyticsService agentUsageAnalyticsService)
    {
        _agentUsageAnalyticsService = agentUsageAnalyticsService;
    }

    public async Task<UsageAnalyticsResult> GetForUserAsync(Guid userId, UsageAnalyticsRequest input, CancellationToken ct = default)
    {
        var (from, toExclusive) = NormalizeRange(input.From, input.To);
        var dashboard = await _agentUsageAnalyticsService.GetDashboardAsync(
            userId,
            new AgentUsageAnalyticsRequest(from, toExclusive.AddDays(-1)),
            ct);

        return new UsageAnalyticsResult(
            from,
            toExclusive,
            dashboard.TotalTokens,
            dashboard.TotalCredits,
            new UsageCostBreakdown(0, 0, 0, "BYOK", false),
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
}
