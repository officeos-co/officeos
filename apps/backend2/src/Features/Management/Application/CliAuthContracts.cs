namespace OffceOs.Application.Features.Management;

public sealed record CliDeviceCodeRequest(string? RunnerName);

public sealed record CliDeviceCodeResult(
    string DeviceCode,
    string UserCode,
    string VerificationUri,
    string VerificationUriComplete,
    DateTime ExpiresAt,
    int IntervalSeconds);

public sealed record CliDeviceTokenResult(
    string Status,
    string? AccessToken,
    DateTime? ExpiresAt,
    int IntervalSeconds);

public interface ICliAuthService
{
    Task<CliDeviceCodeResult> CreateDeviceCodeAsync(CliDeviceCodeRequest request, CancellationToken ct = default);
    Task AuthorizeDeviceCodeAsync(string userCode, Guid userId, CancellationToken ct = default);
    Task<CliDeviceTokenResult> PollTokenAsync(string deviceCode, CancellationToken ct = default);
}
