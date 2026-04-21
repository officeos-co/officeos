namespace EnterpriseAgentOs.Domain.Providers;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default);
    Task<ProviderDto?> ConfigureAsync(string name, string apiKey, CancellationToken ct = default);
    Task<bool> ClearAsync(string name, CancellationToken ct = default);
    Task<string?> GetDecryptedKeyAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Returns the decrypted API key for LLM dispatch. Returns null if no key is configured.
    /// </summary>
    Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default);
}
