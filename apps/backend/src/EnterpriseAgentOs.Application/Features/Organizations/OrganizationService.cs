namespace EnterpriseAgentOs.Application.Features.Organizations;

internal sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationService(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<OrgMemberRecord> InviteMemberAsync(
        Guid callerUserId, string callerEmail, string? callerName,
        string memberEmail, string? role, CancellationToken ct = default)
    {
        var org = await _organizationRepository.GetOrCreateDefaultAsync(callerUserId, callerEmail, callerName, ct);
        if (org.OwnerUserId != callerUserId)
            throw new InvalidOperationException("Only the organization owner may invite members.");

        if (string.IsNullOrWhiteSpace(memberEmail) || !memberEmail.Contains('@'))
            throw new InvalidOperationException("Valid email required.");

        var validRole = role ?? OrgRole.Member.ToStorageString();
        if (validRole != OrgRole.Admin.ToStorageString() && validRole != OrgRole.Member.ToStorageString())
            throw new InvalidOperationException("Role must be 'Admin' or 'Member'.");

        return await _organizationRepository.AddMemberAsync(org.Id, memberEmail.Trim().ToLowerInvariant(), validRole, MemberStatus.Invited.ToStorageString(), null, ct);
    }

    public async Task<bool> RemoveMemberAsync(
        Guid callerUserId, string callerEmail, string? callerName,
        Guid memberId, CancellationToken ct = default)
    {
        var org = await _organizationRepository.GetOrCreateDefaultAsync(callerUserId, callerEmail, callerName, ct);
        if (org.OwnerUserId != callerUserId)
            throw new InvalidOperationException("Only the organization owner may remove members.");

        var members = await _organizationRepository.ListMembersAsync(org.Id, ct);
        var target = members.FirstOrDefault(m => m.Id == memberId);
        if (target is null) return false;

        if (target.Role == OrgRole.Owner)
            throw new InvalidOperationException("Cannot remove the owner.");

        return await _organizationRepository.RemoveMemberAsync(memberId, ct);
    }

    public async Task<OrganizationRecord> RenameAsync(
        Guid callerUserId, string callerEmail, string? callerName,
        string name, CancellationToken ct = default)
    {
        var org = await _organizationRepository.GetOrCreateDefaultAsync(callerUserId, callerEmail, callerName, ct);
        if (org.OwnerUserId != callerUserId)
            throw new InvalidOperationException("Only the organization owner may rename the org.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Name required.");

        return await _organizationRepository.RenameAsync(org.Id, name.Trim(), ct);
    }

    public async Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid callerUserId, string callerEmail, string? callerName,
        CancellationToken ct = default)
    {
        var org = await _organizationRepository.GetOrCreateDefaultAsync(callerUserId, callerEmail, callerName, ct);
        return await _organizationRepository.ListMembersAsync(org.Id, ct);
    }
}
