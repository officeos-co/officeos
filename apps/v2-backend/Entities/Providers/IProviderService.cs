namespace EnterpriseAgentOs.Api.Entities.Providers;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default);
    Task<ProviderDto?> ConfigureAsync(string name, string apiKey, CancellationToken ct = default);
    Task<bool> ClearAsync(string name, CancellationToken ct = default);
    Task<string?> GetDecryptedKeyAsync(string name, CancellationToken ct = default);
}
