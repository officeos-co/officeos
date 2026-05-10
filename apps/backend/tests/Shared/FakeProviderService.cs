using OffceOs.Application.Features.Providers;
using OffceOs.Domain.Features.Providers;

namespace OffceOs.Tests.Shared;

public sealed class FakeProviderService : IProviderService
{
    private readonly ProviderAuthResult? _auth;
    private readonly bool _modelAllowed;

    public FakeProviderService(ProviderAuthResult? auth = null, bool modelAllowed = true)
    {
        _auth = auth ?? new ProviderAuthResult(
            ProviderAuthKind.ApiKey,
            new Dictionary<string, string> { ["apiKey"] = "test-key" });
        _modelAllowed = modelAllowed;
    }

    public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProviderResult>>([]);

    public Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default) =>
        ListAsync(ct);

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(_auth?.Kind is ProviderAuthKind.ApiKey or ProviderAuthKind.AwsBedrockApiKey or ProviderAuthKind.AzureApiKey
            ? _auth.Get("apiKey")
            : null);

    public Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default) =>
        GetApiKeyForDispatchAsync(name, ct);

    public Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default) =>
        Task.FromResult(_auth);

    public Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default) =>
        Task.FromResult(_modelAllowed);
}
