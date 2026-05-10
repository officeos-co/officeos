namespace OffceOs.Application.Features.Agents;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Returns the API key for LLM dispatch from environment/config. Returns null if no key is configured.
    /// </summary>
    Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default);
    Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default);
    Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default);
}

public sealed record ProviderResult(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt,
    IReadOnlyList<ProviderModelResult> Models);

public sealed record ProviderModelResult(
    string Id,
    string DisplayName,
    int CostWeight);
