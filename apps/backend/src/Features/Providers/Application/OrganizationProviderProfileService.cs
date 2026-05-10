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
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        await _providerEnterprisePolicy.RequireEnterpriseOrganizationAsync(organizationId, ct);
        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException("Provider is required.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Provider API key is required.");

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var definition = ProviderRegistry.Get(normalizedProvider);
        if (definition is null && !ProviderRegistry.IsCustomProvider(normalizedProvider))
            throw new InvalidOperationException($"Provider '{normalizedProvider}' is not supported.");

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
            EncryptedApiKey = _credentialProtector.Protect(new Dictionary<string, string> { ["apiKey"] = apiKey }),
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
}
