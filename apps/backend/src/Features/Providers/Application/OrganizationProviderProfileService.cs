namespace OffceOs.Application.Features.Providers;

internal sealed class OrganizationProviderProfileService : IOrganizationProviderProfileService
{
    private readonly IOrganizationProviderProfileRepository _organizationProviderProfileRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly ProviderEnterprisePolicy _providerEnterprisePolicy;

    public OrganizationProviderProfileService(
        IOrganizationProviderProfileRepository organizationProviderProfileRepository,
        IOrganizationRepository organizationRepository,
        CredentialProtector credentialProtector,
        ProviderEnterprisePolicy providerEnterprisePolicy)
    {
        _organizationProviderProfileRepository = organizationProviderProfileRepository;
        _organizationRepository = organizationRepository;
        _credentialProtector = credentialProtector;
        _providerEnterprisePolicy = providerEnterprisePolicy;
    }

    public async Task<IReadOnlyList<OrganizationProviderProfileRecord>> ListAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        await _providerEnterprisePolicy.RequireEnterpriseOrganizationAsync(organizationId, ct);
        return await _organizationProviderProfileRepository.ListAsync(
            new OrganizationProviderProfileFilter { OrganizationId = organizationId },
            ct);
    }

    public async Task<OrganizationProviderProfileRecord> SaveAsync(
        Guid actorUserId,
        Guid organizationId,
        string provider,
        string displayName,
        IReadOnlyList<string> allowedModels,
        string apiKey,
        bool enabled,
        CancellationToken ct = default)
    {
        return await SaveNativeAuthAsync(
            actorUserId,
            organizationId,
            provider,
            displayName,
            allowedModels,
            ProviderAuthKind.ApiKey,
            new Dictionary<string, string> { ["apiKey"] = apiKey },
            enabled,
            ct);
    }

    public async Task<OrganizationProviderProfileRecord> SaveNativeAuthAsync(
        Guid actorUserId,
        Guid organizationId,
        string provider,
        string displayName,
        IReadOnlyList<string> allowedModels,
        ProviderAuthKind authKind,
        IReadOnlyDictionary<string, string> credentials,
        bool enabled,
        CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        await _providerEnterprisePolicy.RequireEnterpriseOrganizationAsync(organizationId, ct);
        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException("Provider is required.");

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var definition = ProviderRegistry.Get(normalizedProvider);
        if (definition is null && !ProviderRegistry.IsCustomProvider(normalizedProvider))
            throw new InvalidOperationException($"Provider '{normalizedProvider}' is not supported.");

        var normalizedCredentials = ValidateCredentials(normalizedProvider, authKind, credentials);

        var modelList = allowedModels
            .Select(model => model.Trim())
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (definition?.RequiresPinnedModels == true && modelList.Count == 0)
            throw new InvalidOperationException($"Provider '{normalizedProvider}' requires pinned allowed models.");

        if (definition is not null)
        {
            var supportedModels = definition.Models.Select(model => model.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unsupportedModel = modelList.FirstOrDefault(model => !supportedModels.Contains(model));
            if (unsupportedModel is not null)
                throw new InvalidOperationException($"Model '{unsupportedModel}' is not supported by provider '{normalizedProvider}'.");
        }

        var record = new OrganizationProviderProfileRecord
        {
            OrganizationId = organizationId,
            Provider = normalizedProvider,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedProvider : displayName.Trim(),
            AllowedModelsJson = JsonSerializer.Serialize(modelList),
            EncryptedApiKey = _credentialProtector.Protect(normalizedCredentials),
            Enabled = enabled,
            ConfiguredAt = DateTime.UtcNow,
        };
        return await _organizationProviderProfileRepository.UpsertAsync(record, ct);
    }

    public async Task<bool> DeleteAsync(Guid actorUserId, Guid organizationId, string provider, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        await _providerEnterprisePolicy.RequireEnterpriseOrganizationAsync(organizationId, ct);
        return await _organizationProviderProfileRepository.DeleteAsync(
            new OrganizationProviderProfileFilter { OrganizationId = organizationId, Provider = provider.Trim().ToLowerInvariant() },
            ct);
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Organization not found.");
    }

    private static Dictionary<string, string> ValidateCredentials(
        string provider,
        ProviderAuthKind authKind,
        IReadOnlyDictionary<string, string> credentials)
    {
        var normalized = credentials
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Trim(), StringComparer.OrdinalIgnoreCase);
        normalized["authKind"] = authKind.ToStorageString();

        switch (provider, authKind)
        {
            case (_, ProviderAuthKind.Gateway):
                Require(normalized, "baseUrl");
                break;
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsEnvironment):
                Require(normalized, "awsRegion");
                break;
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsProfile):
                Require(normalized, "awsRegion");
                Require(normalized, "awsProfile");
                break;
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsAccessKey):
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsIam):
                Require(normalized, "awsAccessKeyId");
                Require(normalized, "awsSecretAccessKey");
                Require(normalized, "awsRegion");
                break;
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsBedrockApiKey):
                Require(normalized, "apiKey");
                Require(normalized, "awsRegion");
                break;
            case (ProviderRegistry.GoogleVertexProviderSlug, ProviderAuthKind.GoogleServiceAccountFile):
                Require(normalized, "credentialsPath");
                Require(normalized, "projectId");
                Require(normalized, "location");
                break;
            case (ProviderRegistry.GoogleVertexProviderSlug, ProviderAuthKind.GoogleServiceAccount):
                Require(normalized, "serviceAccountJson");
                Require(normalized, "projectId");
                Require(normalized, "location");
                break;
            case (ProviderRegistry.GoogleVertexProviderSlug, ProviderAuthKind.GoogleApplicationDefault):
                Require(normalized, "projectId");
                Require(normalized, "location");
                break;
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureDefaultCredential):
                RequireFoundryEndpoint(normalized);
                break;
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureEntraClientSecret):
                Require(normalized, "tenantId");
                Require(normalized, "clientId");
                Require(normalized, "clientSecret");
                RequireFoundryEndpoint(normalized);
                break;
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureManagedIdentity):
                RequireFoundryEndpoint(normalized);
                break;
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureApiKey):
                Require(normalized, "apiKey");
                RequireFoundryEndpoint(normalized);
                break;
            case (_, ProviderAuthKind.ApiKey):
                Require(normalized, "apiKey");
                break;
            default:
                throw new InvalidOperationException($"Authentication kind '{authKind.ToStorageString()}' is not supported for provider '{provider}'.");
        }

        return normalized;
    }

    private static void Require(IReadOnlyDictionary<string, string> credentials, string key)
    {
        if (!credentials.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Provider credential '{key}' is required.");
    }

    private static void RequireFoundryEndpoint(IReadOnlyDictionary<string, string> credentials)
    {
        if (!credentials.ContainsKey("resource") && !credentials.ContainsKey("baseUrl") && !credentials.ContainsKey("endpoint"))
            throw new InvalidOperationException("Provider credential 'resource' or 'baseUrl' is required.");
    }
}
