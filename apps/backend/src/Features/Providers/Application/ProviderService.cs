namespace OffceOs.Application.Features.Providers;

internal sealed class ProviderService : IProviderService
{
    private readonly IProviderResourceRepository _providerResourceRepository;
    private readonly CredentialProtector _credentialProtector;

    public ProviderService(
        IProviderResourceRepository providerResourceRepository,
        CredentialProtector credentialProtector)
    {
        _providerResourceRepository = providerResourceRepository;
        _credentialProtector = credentialProtector;
    }

    public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProviderResult>>([]);

    public async Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default)
    {
        if (!workspaceId.HasValue)
            return [];

        var providers = await _providerResourceRepository.ListAsync(workspaceId.Value, ct);
        return providers.Select(ToProviderResult).ToList();
    }

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    public async Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        var auth = await GetAuthForDispatchAsync(name, workspaceId, ct);
        return auth?.Kind is ProviderAuthKind.ApiKey or ProviderAuthKind.AwsBedrockApiKey or ProviderAuthKind.AzureApiKey
            ? auth.Get("apiKey")
            : null;
    }

    public async Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        if (!workspaceId.HasValue)
            return null;

        var provider = await _providerResourceRepository.GetByNameAsync(workspaceId.Value, name, ct);
        if (provider is null || !provider.Enabled || string.IsNullOrWhiteSpace(provider.EncryptedCredentialsJson))
            return null;

        var credentials = _credentialProtector.Unprotect(provider.EncryptedCredentialsJson);
        return new ProviderAuthResult(provider.AuthKind.ToProviderAuthKind(), credentials);
    }

    public async Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default)
    {
        if (!workspaceId.HasValue)
            return false;

        var resource = await _providerResourceRepository.GetByNameAsync(workspaceId.Value, provider, ct);
        if (resource is null || !resource.Enabled)
            return false;

        var effectiveModel = string.IsNullOrWhiteSpace(model) ? ProviderRegistry.DefaultModel : model.Trim();
        if (effectiveModel.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase))
            return resource.Models.Count > 0;

        return resource.Models.Any(allowed => allowed.Equals(effectiveModel, StringComparison.OrdinalIgnoreCase));
    }

    private static ProviderResult ToProviderResult(ProviderResourceRecord resource)
    {
        var definition = ProviderRegistry.Get(resource.Type);
        var displayName = string.IsNullOrWhiteSpace(resource.DisplayName)
            ? definition?.DisplayName ?? resource.Name
            : resource.DisplayName;
        var models = resource.Models.Count == 0 && definition is not null
            ? definition.Models.Select(model => model.Id).ToList()
            : resource.Models;

        return new ProviderResult(
            resource.Id,
            resource.Name,
            displayName,
            resource.Enabled && !string.IsNullOrWhiteSpace(resource.EncryptedCredentialsJson),
            resource.UpdatedAt,
            models.Select(model =>
            {
                var definitionModel = definition?.Models.FirstOrDefault(item => item.Id.Equals(model, StringComparison.OrdinalIgnoreCase));
                return new ProviderModelResult(model, definitionModel?.DisplayName ?? model, definitionModel?.CostWeight ?? 1);
            }).ToList());
    }
}
