namespace EnterpriseAgentOs.Api.Entities.Analytics;

public interface IAnalyticsService
{
    Task CaptureAsync(
        string distinctId,
        string eventName,
        IReadOnlyDictionary<string, object?>? properties,
        CancellationToken ct = default);

    Task IdentifyAsync(
        string distinctId,
        IReadOnlyDictionary<string, object?>? traits,
        CancellationToken ct = default);
}
