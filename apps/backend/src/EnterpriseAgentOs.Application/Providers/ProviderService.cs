namespace EnterpriseAgentOs.Application.Providers;

//todo shouldnt that be like a service in the domain?
internal sealed class ProviderService : IProviderService
{
    private readonly IProviderRepository _providerRepository;
    private readonly ProviderKeyProtector _providerKeyProtector;
    private readonly PlatformKeysConfig _platformKeysConfig;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(IProviderRepository repository, ProviderKeyProtector protector,
        PlatformKeysConfig platformKeys, ILogger<ProviderService> logger)
    {
        _providerRepository = repository;
        _providerKeyProtector = protector;
        _platformKeysConfig = platformKeys;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _providerRepository.ListAsync(ct);
        _logger.LogDebug("Listed {Count} providers", records.Count);
        return records.Select(ToDtoWithPlatform).ToList();
    }

    private ProviderDto ToDtoWithPlatform(ProviderRecord record)
    {
        var configured = record.Configured || HasPlatformKey(record.Name);
        return new(record.Id, record.Name, record.DisplayName, configured, record.ConfiguredAt);
    }

    private bool HasPlatformKey(string name) => name.ToLowerInvariant() switch
    {
        "anthropic" => !string.IsNullOrWhiteSpace(_platformKeysConfig.AnthropicApiKey),
        "google" => !string.IsNullOrWhiteSpace(_platformKeysConfig.GeminiApiKey),
        "xai" => !string.IsNullOrWhiteSpace(_platformKeysConfig.XaiApiKey),
        _ => false,
    };

    public async Task<ProviderDto?> ConfigureAsync(string name, string apiKey, CancellationToken ct = default)
    {
        var record = await _providerRepository.GetByNameAsync(name, ct);
        if (record is null)
        {
            _logger.LogWarning("Provider {ProviderName} not found for configuration", name);
            return null;
        }

        record.EncryptedApiKey = _providerKeyProtector.Protect(apiKey.Trim());
        record.ConfiguredAt = DateTime.UtcNow;
        await _providerRepository.SaveAsync(record, ct);
        _logger.LogInformation("Provider {ProviderName} API key configured", name);
        return ToDto(record);
    }

    public Task<bool> ClearAsync(string name, CancellationToken ct = default)
    {
        _logger.LogInformation("Clearing API key for provider {ProviderName}", name);
        return _providerRepository.ClearKeyAsync(name, ct);
    }

    public async Task<string?> GetDecryptedKeyAsync(string name, CancellationToken ct = default)
    {
        var record = await _providerRepository.GetByNameAsync(name, ct);
        if (record?.EncryptedApiKey is null)
        {
            _logger.LogDebug("No API key configured for provider {ProviderName}", name);
            return null;
        }
        return _providerKeyProtector.Unprotect(record.EncryptedApiKey);
    }

    public async Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default)
    {
        // Check user-configured key in DB first
        var record = await _providerRepository.GetByNameAsync(name, ct);
        if (record?.EncryptedApiKey is not null)
            return _providerKeyProtector.Unprotect(record.EncryptedApiKey);

        // Fall back to platform keys from config
        return GetPlatformKey(name);
    }

    private string? GetPlatformKey(string name) => name.ToLowerInvariant() switch
    {
        "anthropic" => NullIfEmpty(_platformKeysConfig.AnthropicApiKey),
        "google" => NullIfEmpty(_platformKeysConfig.GeminiApiKey),
        "xai" => NullIfEmpty(_platformKeysConfig.XaiApiKey),
        "openai" => NullIfEmpty(_platformKeysConfig.OpenAiApiKey),
        _ => null,
    };

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static ProviderDto ToDto(ProviderRecord record) =>
        new(record.Id, record.Name, record.DisplayName, record.Configured, record.ConfiguredAt);
}
