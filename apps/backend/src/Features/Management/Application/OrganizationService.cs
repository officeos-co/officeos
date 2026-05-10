namespace OffceOs.Application.Features.Management;

internal sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IPublisher _publisher;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IPublisher publisher)
    {
        _organizationRepository = organizationRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _publisher = publisher;
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

        var parsedRole = (role ?? "Editor").ToOrgRole();
        if (parsedRole is not (OrgRole.Admin or OrgRole.Editor or OrgRole.Viewer))
            throw new InvalidOperationException("Role must be 'Admin', 'Editor', or 'Viewer'.");

        var member = OrgMemberRecord.Invite(org.Id, memberEmail, parsedRole);
        var created = await _organizationRepository.AddMemberAsync(member, ct);
        await _publisher.Publish(new OrganizationMemberInvitedEvent(
            org.Id,
            callerUserId,
            created.Id,
            created.Email,
            created.Role.ToString()), ct);
        return created;
    }

    public async Task<IReadOnlyList<OrganizationInviteRecord>> ListPendingInvitesAsync(
        Guid callerUserId,
        string callerEmail,
        CancellationToken ct = default)
    {
        _ = callerUserId;
        return await _organizationRepository.ListPendingInvitesForEmailAsync(callerEmail, ct);
    }

    public async Task<OrgMemberRecord> AcceptInviteAsync(
        Guid callerUserId,
        string callerEmail,
        Guid memberId,
        CancellationToken ct = default)
    {
        var member = await _organizationRepository.AcceptInviteAsync(memberId, callerUserId, callerEmail, ct);
        await _publisher.Publish(new OrganizationMemberInviteAcceptedEvent(
            member.OrganizationId,
            callerUserId,
            member.Id,
            member.Email,
            member.Role.ToString()), ct);
        return member;
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

        var removed = await _organizationRepository.RemoveMemberAsync(memberId, ct);
        if (removed)
        {
            await _publisher.Publish(new OrganizationMemberRemovedEvent(
                org.Id,
                callerUserId,
                target.Id,
                target.UserId,
                target.Email,
                target.Role.ToString()), ct);
        }

        return removed;
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

        var previousName = org.Name;
        var renamed = await _organizationRepository.RenameAsync(org.Id, name.Trim(), ct);
        await _publisher.Publish(new OrganizationRenamedEvent(org.Id, callerUserId, previousName, renamed.Name), ct);
        return renamed;
    }

    public async Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid callerUserId, string callerEmail, string? callerName,
        CancellationToken ct = default)
    {
        var org = await _organizationRepository.GetOrCreateDefaultAsync(callerUserId, callerEmail, callerName, ct);
        return await _organizationRepository.ListMembersAsync(org.Id, ct);
    }
}
