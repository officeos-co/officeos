namespace EnterpriseAgentOs.Domain.Features.Analytics;

public interface IUsageAnalyticsService
{
    Task<UsageAnalyticsDto> GetForUserAsync(Guid userId, UsageAnalyticsInput input, CancellationToken ct = default);
}
