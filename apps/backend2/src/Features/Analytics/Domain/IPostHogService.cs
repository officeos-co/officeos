namespace OffceOs.Domain.Features.Analytics;

public interface IPostHogService
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
