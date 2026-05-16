using OffceOs.Configuration;
using OffceOs.Domain.Features.Management;
using OffceOs.Domain.Common.ValueObjects;
namespace OffceOs.Application.Features.Management;

internal sealed class CliAuthService : ICliAuthService
{
    private static readonly TimeSpan DeviceCodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CliTokenLifetime = TimeSpan.FromDays(30);
    private const int PollIntervalSeconds = 5;

    private readonly IDeviceCodeRepository _deviceCodeRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly FrontendConfig _frontendConfig;

    public CliAuthService(
        IDeviceCodeRepository deviceCodeRepository,
        ISessionRepository sessionRepository,
        FrontendConfig frontendConfig)
    {
        _deviceCodeRepository = deviceCodeRepository;
        _sessionRepository = sessionRepository;
        _frontendConfig = frontendConfig;
    }

    public async Task<CliDeviceCodeResult> CreateDeviceCodeAsync(CliDeviceCodeRequest request, CancellationToken ct = default)
    {
        var userCode = GenerateUserCode();
        var deviceCode = $"eaos_dc_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}";
        var verificationUri = BuildVerificationUri(null);
        var verificationUriComplete = BuildVerificationUri(userCode);
        var expiresAt = DateTime.UtcNow.Add(DeviceCodeLifetime);

        await _deviceCodeRepository.AddAsync(new DeviceCodeRecord
        {
            DeviceCode = deviceCode,
            UserCode = NormalizeUserCode(userCode),
            RunnerName = string.IsNullOrWhiteSpace(request.RunnerName) ? null : request.RunnerName.Trim(),
            ExpiresAt = expiresAt,
        }, ct);

        return new CliDeviceCodeResult(
            deviceCode,
            FormatUserCode(userCode),
            verificationUri,
            verificationUriComplete,
            expiresAt,
            PollIntervalSeconds);
    }

    public async Task AuthorizeDeviceCodeAsync(string userCode, Guid userId, CancellationToken ct = default)
    {
        var record = await _deviceCodeRepository.GetByUserCodeAsync(userCode, ct)
            ?? throw new InvalidOperationException("Device code not found.");

        if (record.ExpiresAt <= DateTime.UtcNow)
        {
            record.Status = DeviceCodeStatus.Expired;
            await _deviceCodeRepository.UpdateAsync(record, ct);
            throw new InvalidOperationException("Device code expired.");
        }

        if (record.Status != DeviceCodeStatus.Pending)
            throw new InvalidOperationException("Device code is no longer pending.");

        record.UserId = userId;
        record.Status = DeviceCodeStatus.Authorized;
        await _deviceCodeRepository.UpdateAsync(record, ct);
    }

    public async Task<CliDeviceTokenResult> PollTokenAsync(string deviceCode, CancellationToken ct = default)
    {
        var record = await _deviceCodeRepository.GetByDeviceCodeAsync(deviceCode, ct)
            ?? throw new InvalidOperationException("Device code not found.");

        record.LastPolledAt = DateTime.UtcNow;

        if (record.ExpiresAt <= DateTime.UtcNow)
        {
            record.Status = DeviceCodeStatus.Expired;
            await _deviceCodeRepository.UpdateAsync(record, ct);
            return new CliDeviceTokenResult("expired", null, null, PollIntervalSeconds);
        }

        if (record.Status == DeviceCodeStatus.Pending)
        {
            await _deviceCodeRepository.UpdateAsync(record, ct);
            return new CliDeviceTokenResult("pending", null, null, PollIntervalSeconds);
        }

        if (record.UserId is null)
            throw new InvalidOperationException("Authorized device code has no user.");

        var token = await CreateSessionTokenAsync(record.UserId.Value, CliTokenLifetime, ct);
        record.Status = DeviceCodeStatus.Expired;
        await _deviceCodeRepository.UpdateAsync(record, ct);
        return new CliDeviceTokenResult("authorized", token, DateTime.UtcNow.Add(CliTokenLifetime), PollIntervalSeconds);
    }

    private string BuildVerificationUri(string? userCode)
    {
        var baseUri = new Uri(new Uri(_frontendConfig.Origin), "/cli/activate");
        if (string.IsNullOrWhiteSpace(userCode))
            return baseUri.ToString();

        return $"{baseUri}?code={Uri.EscapeDataString(FormatUserCode(userCode))}";
    }

    private static string GenerateUserCode() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToUpperInvariant();

    private static string NormalizeUserCode(string userCode) =>
        userCode.Replace("-", "", StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

    private static string FormatUserCode(string userCode)
    {
        var normalized = NormalizeUserCode(userCode);
        return normalized.Length <= 4 ? normalized : $"{normalized[..4]}-{normalized[4..]}";
    }

    private async Task<string> CreateSessionTokenAsync(Guid userId, TimeSpan lifetime, CancellationToken ct)
    {
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = SessionTokenHasher.Hash(sessionToken);
        await _sessionRepository.CreateAsync(userId, tokenHash, DateTime.UtcNow.Add(lifetime), ct);
        return sessionToken;
    }
}
