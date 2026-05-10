namespace OffceOs.Application.Features.Providers;

internal sealed class ProviderService : IProviderService
{
    private readonly PlatformKeysConfig _platformKeysConfig;
    private readonly CustomLlmProviderConfig _customLlmProviderConfig;
    private readonly IOrganizationProviderProfileRepository _organizationProviderProfileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly ProviderEnterprisePolicy _providerEnterprisePolicy;

    public ProviderService(
        PlatformKeysConfig platformKeys,
        CustomLlmProviderConfig customLlmProviderConfig,
        IOrganizationProviderProfileRepository organizationProviderProfileRepository,
        IWorkspaceRepository workspaceRepository,
        CredentialProtector credentialProtector,
        ProviderEnterprisePolicy providerEnterprisePolicy)
    {
        _platformKeysConfig = platformKeys;
        _customLlmProviderConfig = customLlmProviderConfig;
        _organizationProviderProfileRepository = organizationProviderProfileRepository;
        _workspaceRepository = workspaceRepository;
        _credentialProtector = credentialProtector;
        _providerEnterprisePolicy = providerEnterprisePolicy;
    }

    public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default)
    {
        var list = ProviderRegistry.DashboardProviders
            .Select(def => new ProviderResult(
                DeterministicGuid(def.Slug),
                def.Slug,
                def.DisplayName,
                HasPlatformKey(def.Slug),
                null,
                def.Models.Select(m => new ProviderModelResult(m.Id, m.DisplayName, m.CostWeight)).ToList()))
            .ToList();

        list.Add(new ProviderResult(
            DeterministicGuid(ProviderRegistry.CustomProviderSlug),
            ProviderRegistry.CustomProviderSlug,
            _customLlmProviderConfig.EffectiveDisplayName,
            _customLlmProviderConfig.IsConfigured,
            null,
            GetCustomModels()));

        return Task.FromResult<IReadOnlyList<ProviderResult>>(list);
    }

    public async Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default)
    {
        var organizationId = await GetOrganizationIdAsync(workspaceId, ct);
        if (!organizationId.HasValue)
            return await ListAsync(ct);

        if (!await _providerEnterprisePolicy.IsEnterpriseOrganizationAsync(organizationId.Value, ct))
            return await ListAsync(ct);

        var profiles = await _organizationProviderProfileRepository.ListAsync(
            new OrganizationProviderProfileFilter { OrganizationId = organizationId.Value, Enabled = true },
            ct);
        if (profiles.Count == 0)
            return await ListAsync(ct);

        return profiles.Select(ToProviderResult).ToList();
    }

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default)
    {
        if (ProviderRegistry.IsCustomProvider(name))
            return Task.FromResult(_customLlmProviderConfig.IsConfigured
                ? _customLlmProviderConfig.ApiKeyOrNull ?? string.Empty
                : null);

        var key = _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName);
        return Task.FromResult(key);
    }

    public async Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        var auth = await GetAuthForDispatchAsync(name, workspaceId, ct);
        return auth?.Kind is ProviderAuthKind.ApiKey or ProviderAuthKind.AwsBedrockApiKey or ProviderAuthKind.AzureApiKey
            ? auth.Get("apiKey")
            : null;
    }

    public async Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        var organizationId = await GetOrganizationIdAsync(workspaceId, ct);
        if (organizationId.HasValue &&
            await _providerEnterprisePolicy.IsEnterpriseOrganizationAsync(organizationId.Value, ct))
        {
            var profile = await _organizationProviderProfileRepository.GetByAsync(
                new OrganizationProviderProfileFilter
                {
                    OrganizationId = organizationId.Value,
                    Provider = name.Trim().ToLowerInvariant(),
                    Enabled = true,
                },
                ct);
            if (profile is not null)
                return ToProviderAuthResult(_credentialProtector.Unprotect(profile.EncryptedApiKey));
        }

        var apiKey = await GetApiKeyForDispatchAsync(name, ct);
        return apiKey is null
            ? null
            : new ProviderAuthResult(ProviderAuthKind.ApiKey, new Dictionary<string, string> { ["apiKey"] = apiKey });
    }

    public async Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default)
    {
        var configured = await ListForWorkspaceAsync(workspaceId, ct);
        var result = configured.FirstOrDefault(p => p.Name.Equals(provider, StringComparison.OrdinalIgnoreCase) && p.Configured);
        if (result is null)
            return false;

        var effectiveModel = string.IsNullOrWhiteSpace(model) ? ProviderRegistry.DefaultModel : model.Trim();
        if (effectiveModel.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase))
            return result.Models.Count > 0;

        return result.Models.Any(m => m.Id.Equals(effectiveModel, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasPlatformKey(string name) =>
        _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName) is not null;

    private IReadOnlyList<ProviderModelResult> GetCustomModels() =>
        _customLlmProviderConfig.IsConfigured
            ? new[]
            {
                new ProviderModelResult(
                    _customLlmProviderConfig.ModelId.Trim(),
                    _customLlmProviderConfig.EffectiveModelDisplayName,
                    _customLlmProviderConfig.EffectiveCostWeight),
            }
            : [];

    private async Task<Guid?> GetOrganizationIdAsync(Guid? workspaceId, CancellationToken ct)
    {
        if (!workspaceId.HasValue)
            return null;

        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId.Value }, ct);
        return workspace?.OrganizationId;
    }

    private static ProviderResult ToProviderResult(OrganizationProviderProfileRecord profile)
    {
        var definition = ProviderRegistry.Get(profile.Provider);
        var models = ParseModels(profile.AllowedModelsJson);
        if (models.Count == 0 && definition is not null)
            models = definition.Models.Select(m => m.Id).ToList();

        return new ProviderResult(
            profile.Id,
            profile.Provider,
            string.IsNullOrWhiteSpace(profile.DisplayName) ? definition?.DisplayName ?? profile.Provider : profile.DisplayName,
            profile.Enabled && !string.IsNullOrWhiteSpace(profile.EncryptedApiKey),
            profile.ConfiguredAt,
            models.Select(model =>
            {
                var definitionModel = definition?.Models.FirstOrDefault(m => m.Id.Equals(model, StringComparison.OrdinalIgnoreCase));
                return new ProviderModelResult(model, definitionModel?.DisplayName ?? model, definitionModel?.CostWeight ?? 1);
            }).ToList());
    }

    private static IReadOnlyList<string> ParseModels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.ValueKind == JsonValueKind.Array
                ? parsed.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static ProviderAuthResult ToProviderAuthResult(IReadOnlyDictionary<string, string> credentials)
    {
        var kind = credentials.TryGetValue("authKind", out var authKind) && !string.IsNullOrWhiteSpace(authKind)
            ? authKind.ToProviderAuthKind()
            : ProviderAuthKind.ApiKey;

        return new ProviderAuthResult(kind, new Dictionary<string, string>(credentials, StringComparer.OrdinalIgnoreCase));
    }

    private static Guid DeterministicGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"provider:{input}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}
