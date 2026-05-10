namespace OffceOs.Application.Features.Providers;

internal sealed class DevelopmentProviderService : IProviderService
{
    private readonly ProviderService _providerService;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly CredentialProtector _credentialProtector;

    public DevelopmentProviderService(
        ProviderService providerService,
        IWorkspaceRepository workspaceRepository,
        IOAuthTokenRepository oauthTokenRepository,
        CredentialProtector credentialProtector)
    {
        _providerService = providerService;
        _workspaceRepository = workspaceRepository;
        _oauthTokenRepository = oauthTokenRepository;
        _credentialProtector = credentialProtector;
    }

    public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default) =>
        _providerService.ListAsync(ct);

    public async Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default)
    {
        var providers = (await _providerService.ListForWorkspaceAsync(workspaceId, ct)).ToList();
        var workspace = await GetWorkspaceAsync(workspaceId, ct);
        if (workspace?.OwnerKind == WorkspaceOwnerKind.Personal && workspace.OwnerUserId.HasValue)
            providers.Add(await ToCodexProviderResultAsync(workspace.OwnerUserId.Value, ct));

        return providers;
    }

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default) =>
        _providerService.GetApiKeyForDispatchAsync(name, ct);

    public async Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        var auth = await GetAuthForDispatchAsync(name, workspaceId, ct);
        return auth?.Kind is ProviderAuthKind.ApiKey or ProviderAuthKind.AwsBedrockApiKey or ProviderAuthKind.AzureApiKey
            ? auth.Get("apiKey")
            : null;
    }

    public async Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        if (name.Equals(ProviderRegistry.OpenAiCodexProviderSlug, StringComparison.OrdinalIgnoreCase))
            return await GetPersonalCodexAuthAsync(workspaceId, ct);

        return await _providerService.GetAuthForDispatchAsync(name, workspaceId, ct);
    }

    public async Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default)
    {
        if (!provider.Equals(ProviderRegistry.OpenAiCodexProviderSlug, StringComparison.OrdinalIgnoreCase))
            return await _providerService.IsModelAllowedAsync(provider, model, workspaceId, ct);

        var auth = await GetPersonalCodexAuthAsync(workspaceId, ct);
        if (auth is null)
            return false;

        var effectiveModel = string.IsNullOrWhiteSpace(model) ? "gpt-5.5" : model.Trim();
        return ProviderRegistry.GetModelIds(ProviderRegistry.OpenAiCodexProviderSlug)
            .Any(id => id.Equals(effectiveModel, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<WorkspaceRecord?> GetWorkspaceAsync(Guid? workspaceId, CancellationToken ct)
    {
        if (!workspaceId.HasValue)
            return null;

        return await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId.Value }, ct);
    }

    private async Task<ProviderResult> ToCodexProviderResultAsync(Guid userId, CancellationToken ct)
    {
        var token = await _oauthTokenRepository.GetByAsync(
            new OAuthTokenFilter
            {
                UserId = userId,
                Provider = OAuthProvider.OpenAiCodex.ToStorageString(),
            },
            ct);
        var definition = ProviderRegistry.Get(ProviderRegistry.OpenAiCodexProviderSlug)!;
        return new ProviderResult(
            DeterministicGuid(ProviderRegistry.OpenAiCodexProviderSlug),
            ProviderRegistry.OpenAiCodexProviderSlug,
            definition.DisplayName,
            token?.EncryptedAccessToken is not null,
            token?.UpdatedAt,
            definition.Models.Select(model => new ProviderModelResult(model.Id, model.DisplayName, model.CostWeight)).ToList());
    }

    private async Task<ProviderAuthResult?> GetPersonalCodexAuthAsync(Guid? workspaceId, CancellationToken ct)
    {
        var workspace = await GetWorkspaceAsync(workspaceId, ct);
        if (workspace?.OwnerKind != WorkspaceOwnerKind.Personal || !workspace.OwnerUserId.HasValue)
            return null;

        var token = await _oauthTokenRepository.GetByAsync(
            new OAuthTokenFilter
            {
                UserId = workspace.OwnerUserId.Value,
                Provider = OAuthProvider.OpenAiCodex.ToStorageString(),
            },
            ct);
        if (string.IsNullOrWhiteSpace(token?.EncryptedAccessToken))
            return null;

        var credentials = _credentialProtector.Unprotect(token.EncryptedAccessToken);
        return new ProviderAuthResult(ProviderAuthKind.CodexChatGptOAuth, credentials);
    }

    private static Guid DeterministicGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"provider:{input}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}
