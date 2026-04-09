namespace EnterpriseAgentOs.Api.Entities.Providers;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default);
    Task<ProviderDto?> ConfigureAsync(Guid id, string apiKey, CancellationToken ct = default);
    Task<bool> ClearAsync(Guid id, CancellationToken ct = default);
    Task<string?> GetDecryptedKeyAsync(string name, CancellationToken ct = default);
}
