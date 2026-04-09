namespace EnterpriseAgentOs.Api.Entities.Providers;

public sealed class ProviderService : IProviderService
{
    private readonly IProviderRepository _repository;
    private readonly ProviderKeyProtector _protector;

    public ProviderService(IProviderRepository repository, ProviderKeyProtector protector)
    {
        _repository = repository;
        _protector = protector;
    }

    public async Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        return records.Select(ToDto).ToList();
    }

    public async Task<ProviderDto?> ConfigureAsync(string name, string apiKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByNameAsync(name, ct);
        if (record is null)
        {
            return null;
        }

        record.EncryptedApiKey = _protector.Protect(apiKey.Trim());
        record.ConfiguredAt = DateTime.UtcNow;
        await _repository.SaveAsync(record, ct);
        return ToDto(record);
    }

    public Task<bool> ClearAsync(string name, CancellationToken ct = default)
    {
        return _repository.ClearKeyAsync(name, ct);
    }

    public async Task<string?> GetDecryptedKeyAsync(string name, CancellationToken ct = default)
    {
        var record = await _repository.GetByNameAsync(name, ct);
        if (record?.EncryptedApiKey is null)
        {
            return null;
        }
        return _protector.Unprotect(record.EncryptedApiKey);
    }

    private static ProviderDto ToDto(ProviderRecord record) =>
        new(record.Id, record.Name, record.DisplayName, record.Configured, record.ConfiguredAt);
}
