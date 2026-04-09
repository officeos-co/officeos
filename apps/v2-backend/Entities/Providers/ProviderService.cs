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

    public async Task<ProviderDto?> ConfigureAsync(Guid id, string apiKey, CancellationToken ct = default)
    {
        var record = await _repository.GetAsync(id, ct);
        if (record is null)
        {
            return null;
        }

        record.EncryptedApiKey = _protector.Protect(apiKey.Trim());
        record.ConfiguredAt = DateTime.UtcNow;
        await _repository.SaveAsync(record, ct);
        return ToDto(record);
    }

    public Task<bool> ClearAsync(Guid id, CancellationToken ct = default)
    {
        return _repository.ClearKeyAsync(id, ct);
    }

    public async Task<string?> GetDecryptedKeyAsync(string name, CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        var record = records.FirstOrDefault(p => p.Name == name);
        if (record?.EncryptedApiKey is null)
        {
            return null;
        }
        return _protector.Unprotect(record.EncryptedApiKey);
    }

    private static ProviderDto ToDto(ProviderRecord record) =>
        new(record.Id, record.Name, record.DisplayName, record.Configured, record.ConfiguredAt);
}
