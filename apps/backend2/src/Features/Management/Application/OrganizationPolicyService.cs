namespace OffceOs.Application.Features.Management;

internal sealed class OrganizationPolicyService : IOrganizationPolicyService
{
    private readonly IOrganizationPolicyProfileRepository _organizationPolicyProfileRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IPublisher _publisher;

    public OrganizationPolicyService(
        IOrganizationPolicyProfileRepository organizationPolicyProfileRepository,
        IOrganizationRepository organizationRepository,
        IWorkspaceRepository workspaceRepository,
        IPublisher publisher)
    {
        _organizationPolicyProfileRepository = organizationPolicyProfileRepository;
        _organizationRepository = organizationRepository;
        _workspaceRepository = workspaceRepository;
        _publisher = publisher;
    }

    public async Task<OrganizationPolicyProfileRecord?> GetEffectiveForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default)
    {
        if (!workspaceId.HasValue)
            return null;

        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId.Value }, ct);
        if (workspace?.OrganizationId is null)
            return null;

        return await _organizationPolicyProfileRepository.GetByAsync(
            new OrganizationPolicyProfileFilter { OrganizationId = workspace.OrganizationId.Value },
            ct);
    }

    public async Task<OrganizationPolicyProfileRecord> GetOrCreateAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        var existing = await _organizationPolicyProfileRepository.GetByAsync(
            new OrganizationPolicyProfileFilter { OrganizationId = organizationId },
            ct);
        if (existing is not null)
            return existing;

        return await _organizationPolicyProfileRepository.SaveAsync(OrganizationPolicyProfileRecord.Default(organizationId), ct);
    }

    public async Task<OrganizationPolicyProfileRecord> UpdateAsync(Guid actorUserId, OrganizationPolicyProfileRecord profile, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, profile.OrganizationId, ct);
        var saved = await _organizationPolicyProfileRepository.SaveAsync(profile, ct);
        await _publisher.Publish(new OrganizationPolicyProfileUpdatedEvent(
            saved.OrganizationId,
            actorUserId,
            saved.ShellToolsEnabled,
            saved.FileWriteToolsEnabled,
            saved.NetworkToolsEnabled,
            saved.BrowserToolsEnabled,
            CountJsonArray(saved.AllowedToolsJson),
            CountJsonArray(saved.DeniedToolsJson),
            CountJsonArray(saved.AllowedIntegrationsJson),
            CountJsonArray(saved.DeniedIntegrationsJson)), ct);
        return saved;
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Organization not found.");
    }

    private static int CountJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return 0;

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.ValueKind == JsonValueKind.Array ? parsed.GetArrayLength() : 0;
        }
        catch
        {
            return 0;
        }
    }
}
