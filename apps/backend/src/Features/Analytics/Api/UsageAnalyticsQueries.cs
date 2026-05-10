namespace OffceOs.Api.Features.Analytics;

[ExtendObjectType(typeof(GraphQLQueries))]
public class UsageAnalyticsQueries
{
    [GraphQLDescription("Returns token usage over time plus backend-calculated spend for an exact date range.")]
    public async Task<UsageAnalyticsResult> GetUsageAnalytics(
        UsageAnalyticsInput input,
        [Service] UserContext user,
        [Service] IUsageAnalyticsService usageAnalytics,
        CancellationToken ct)
    {
        return await usageAnalytics.GetForUserAsync(user.Id, new UsageAnalyticsRequest(input.From, input.To), ct);
    }
}
