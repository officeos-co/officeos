namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the API key for LLM dispatch from environment/config. Returns null if no key is configured.
    /// </summary>
    Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default);
}
