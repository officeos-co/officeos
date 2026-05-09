namespace OffceOs.Application.Features.Management;

internal sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _organizationRepository = organizationRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task<OrganizationOverview> GetOverviewAsync(
        Guid callerUserId, string callerEmail, string? callerName,
        CancellationToken ct = default)
    {
        var org = await _organizationRepository.GetOrCreateDefaultAsync(callerUserId, callerEmail, callerName, ct);
        await _workspaceRepository.EnsureOrganizationDefaultAsync(org.Id, org.OwnerUserId, ct);
        var members = await _organizationRepository.ListMembersAsync(org.Id, ct);
        return new OrganizationOverview(org, members);
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

        var parsedRole = (role ?? "Member").ToOrgRole();
        if (parsedRole != OrgRole.Admin && parsedRole != OrgRole.Member)
            throw new InvalidOperationException("Role must be 'Admin' or 'Member'.");

        var member = OrgMemberRecord.Invite(org.Id, memberEmail, parsedRole);
        return await _organizationRepository.AddMemberAsync(member, ct);
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

        if (target.UserId.HasValue)
        {
            await _workspaceMemberRepository.DeleteAsync(
                new WorkspaceMemberFilter { UserId = target.UserId.Value, OrganizationId = org.Id },
                ct);
        }

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
