namespace EnterpriseAgentOs.Application.Services.Providers;

public sealed class ProviderService : IProviderService
{
    private readonly IProviderRepository _repository;
    private readonly ProviderKeyProtector _protector;
    private readonly EnterpriseAgentOs.Infrastructure.Configuration.PlatformKeysConfig _platformKeys;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(IProviderRepository repository, ProviderKeyProtector protector,
        EnterpriseAgentOs.Infrastructure.Configuration.PlatformKeysConfig platformKeys, ILogger<ProviderService> logger)
    {
        _repository = repository;
        _protector = protector;
        _platformKeys = platformKeys;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        _logger.LogDebug("Listed {Count} providers", records.Count);
        return records.Select(ToDtoWithPlatform).ToList();
    }

    private ProviderDto ToDtoWithPlatform(EnterpriseAgentOs.Domain.Models.ProviderRecord record)
    {
        var configured = record.Configured || HasPlatformKey(record.Name);
        return new(record.Id, record.Name, record.DisplayName, configured, record.ConfiguredAt);
    }

    private bool HasPlatformKey(string name) => name.ToLowerInvariant() switch
    {
        "anthropic" => !string.IsNullOrWhiteSpace(_platformKeys.AnthropicApiKey),
        "google" => !string.IsNullOrWhiteSpace(_platformKeys.GeminiApiKey),
        "xai" => !string.IsNullOrWhiteSpace(_platformKeys.XaiApiKey),
        _ => false,
    };

    public async Task<ProviderDto?> ConfigureAsync(string name, string apiKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByNameAsync(name, ct);
        if (record is null)
        {
            _logger.LogWarning("Provider {ProviderName} not found for configuration", name);
            return null;
        }

        record.EncryptedApiKey = _protector.Protect(apiKey.Trim());
        record.ConfiguredAt = DateTime.UtcNow;
        await _repository.SaveAsync(record, ct);
        _logger.LogInformation("Provider {ProviderName} API key configured", name);
        return ToDto(record);
    }

    public Task<bool> ClearAsync(string name, CancellationToken ct = default)
    {
        _logger.LogInformation("Clearing API key for provider {ProviderName}", name);
        return _repository.ClearKeyAsync(name, ct);
    }

    public async Task<string?> GetDecryptedKeyAsync(string name, CancellationToken ct = default)
    {
        var record = await _repository.GetByNameAsync(name, ct);
        if (record?.EncryptedApiKey is null)
        {
            _logger.LogDebug("No API key configured for provider {ProviderName}", name);
            return null;
        }
        return _protector.Unprotect(record.EncryptedApiKey);
    }

    private static ProviderDto ToDto(EnterpriseAgentOs.Domain.Models.ProviderRecord record) =>
        new(record.Id, record.Name, record.DisplayName, record.Configured, record.ConfiguredAt);
}
