using OffceOs.Features.Providers.Domain;

namespace OffceOs.Features.Providers.Application;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Returns the API key for LLM dispatch from environment/config. Returns null if no key is configured.
    /// </summary>
    Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default);
    Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default);
    Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default);
    Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default);
    Task<ProviderResourceAuthResult> AuthenticateCodexAsync(Guid workspaceId, CodexProviderAuthRequest request, CancellationToken ct = default);
}

public sealed record ProviderResult(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt,
    IReadOnlyList<ProviderModelResult> Models,
    string Phase = ProviderResourcePhaseKinds.Pending,
    string StatusMessage = "",
    string? Account = null,
    DateTime? ExpiresAt = null,
    DateTime? LastValidatedAt = null);

public sealed record ProviderModelResult(
    string Id,
    string DisplayName,
    int CostWeight);

public sealed record CodexProviderAuthRequest(
    string AccessToken,
    string RefreshToken,
    DateTime? ExpiresAt,
    string? AccountEmail,
    string? AccountId,
    string? ClientId,
    string? TokenUrl,
    IReadOnlyList<string>? Scopes);

public sealed record ProviderResourceAuthResult(
    string Kind,
    string Name,
    string Type,
    string Phase,
    string StatusMessage,
    string? Account,
    DateTime? ExpiresAt,
    DateTime? LastValidatedAt);
