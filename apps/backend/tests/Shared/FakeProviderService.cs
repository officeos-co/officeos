using OffceOs.Application.Features.Providers;
using OffceOs.Domain.Features.Providers;

namespace OffceOs.Tests.Shared;

public sealed class FakeProviderService : IProviderService
{
    public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProviderResult>>([]);

    public Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default) =>
        ListAsync(ct);

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default) =>
        Task.FromResult<string?>("test-key");

    public Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default) =>
        GetApiKeyForDispatchAsync(name, ct);

    public Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default) =>
        Task.FromResult<ProviderAuthResult?>(new ProviderAuthResult(
            ProviderAuthKind.ApiKey,
            new Dictionary<string, string> { ["apiKey"] = "test-key" }));

    public Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default) =>
        Task.FromResult(true);
}
