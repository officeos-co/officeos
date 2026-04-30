namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class ProviderService : IProviderService
{
    private readonly IProviderRepository _providerRepository;
    private readonly ProviderKeyProtector _providerKeyProtector;
    private readonly PlatformKeysConfig _platformKeysConfig;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(IProviderRepository repository, ProviderKeyProtector protector,
        PlatformKeysConfig platformKeys, IHostEnvironment hostEnvironment, ILogger<ProviderService> logger)
    {
        _providerRepository = repository;
        _providerKeyProtector = protector;
        _platformKeysConfig = platformKeys;
        _hostEnvironment = hostEnvironment;
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
        // In Development (self-hosted), platform keys are synced to DB at startup,
        // so record.Configured is the source of truth.
        // In Production (SaaS), platform keys are operator-level fallbacks — show as configured
        // without persisting to user rows.
        var configured = _hostEnvironment.IsDevelopment()
            ? record.Configured
            : record.Configured || HasPlatformKey(record.Name);
        return new(record.Id, record.Name, record.DisplayName, configured, record.ConfiguredAt);
    }

    private bool HasPlatformKey(string name) =>
        _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName) is not null;

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
        var record = await _providerRepository.GetByNameAsync(name, ct);
        if (record?.EncryptedApiKey is not null)
            return _providerKeyProtector.Unprotect(record.EncryptedApiKey);

        return _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName);
    }

    public async Task SyncPlatformKeysAsync(CancellationToken ct = default)
    {
        if (!_hostEnvironment.IsDevelopment())
            return;

        foreach (var provider in ProviderRegistry.SeedableProviders)
        {
            var key = _platformKeysConfig.GetKey(provider.PlatformKeyConfigName);
            if (key is null) continue;

            var record = await _providerRepository.GetByNameAsync(provider.Slug, ct);
            if (record is null) continue;
            if (record.EncryptedApiKey is not null) continue;

            record.EncryptedApiKey = _providerKeyProtector.Protect(key);
            record.ConfiguredAt = DateTime.UtcNow;
            await _providerRepository.SaveAsync(record, ct);
            _logger.LogInformation("Synced platform key for provider {ProviderName} from environment", provider.Slug);
        }
    }

    private static ProviderDto ToDto(ProviderRecord record) =>
        new(record.Id, record.Name, record.DisplayName, record.Configured, record.ConfiguredAt);
}
