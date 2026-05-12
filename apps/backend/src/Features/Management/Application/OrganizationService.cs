namespace OffceOs.Application.Features.Management;

internal sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPublisher _publisher;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUserRepository userRepository,
        IPublisher publisher)
    {
        _organizationRepository = organizationRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _userRepository = userRepository;
        _publisher = publisher;
    }

    public async Task<OrganizationOverview> GetCurrentOverviewAsync(
        Guid callerUserId,
        CancellationToken ct = default)
    {
        var org = await GetCurrentOrganizationAsync(callerUserId, ct);
        var members = await _organizationRepository.ListMembersAsync(org.Id, ct);
        return new OrganizationOverview(org, members);
    }

    public Task<OrganizationRecord?> GetOwnedOrganizationAsync(Guid callerUserId, CancellationToken ct = default)
        => _organizationRepository.GetByAsync(new OrganizationFilter { OwnerUserId = callerUserId }, ct);

    public Task<IReadOnlyList<OrganizationRecord>> ListJoinedOrganizationsAsync(Guid callerUserId, CancellationToken ct = default)
        => _organizationRepository.ListForMemberAsync(callerUserId, ct);

    public async Task<OrganizationRecord> EnsureOrganizationAsync(
        Guid callerUserId,
        string callerEmail,
        string? callerName,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByAsync(new UserFilter { Id = callerUserId }, ct)
            ?? throw new InvalidOperationException("User not found.");

        _ = callerEmail;
        _ = callerName;
        var owned = await EnsureOwnedOrganizationAsync(user, ct);
        var current = await GetActiveMembershipOrganizationAsync(callerUserId, user.CurrentOrganizationId, ct);
        if (current is not null)
            return current;

        await _userRepository.SetCurrentOrganizationAsync(callerUserId, owned.Id, ct);
        return owned;
    }

    public async Task<OrganizationRecord> GetCurrentOrganizationAsync(Guid callerUserId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByAsync(new UserFilter { Id = callerUserId }, ct)
            ?? throw new InvalidOperationException("User not found.");

        var current = await GetActiveMembershipOrganizationAsync(callerUserId, user.CurrentOrganizationId, ct);
        if (current is not null)
            return current;

        var owned = await EnsureOwnedOrganizationAsync(user, ct);
        await _userRepository.SetCurrentOrganizationAsync(callerUserId, owned.Id, ct);
        return owned;
    }

    public async Task<OrganizationRecord> CreateOrganizationAsync(
        Guid callerUserId,
        string callerEmail,
        string? callerName,
        string name,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Name required.");

        var user = await _userRepository.GetByAsync(new UserFilter { Id = callerUserId }, ct)
            ?? throw new InvalidOperationException("User not found.");
        _ = callerEmail;
        _ = callerName;
        var organization = await EnsureOwnedOrganizationAsync(user, ct);
        if (organization.OwnerUserId != callerUserId)
            throw new InvalidOperationException("Only the organization owner may set up the organization.");

        var previousName = organization.Name;
        organization.Name = name.Trim();
        organization.Kind = OrganizationKind.Shared;
        var updated = await _organizationRepository.SaveAsync(organization, ct);
        var workspace = await _workspaceRepository.EnsureOrganizationDefaultAsync(updated.Id, callerUserId, ct);
        await _workspaceRepository.SetCurrentAsync(callerUserId, workspace.Id, ct);
        await _userRepository.SetCurrentOrganizationAsync(callerUserId, updated.Id, ct);

        if (!string.Equals(previousName, updated.Name, StringComparison.Ordinal))
            await _publisher.Publish(new OrganizationRenamedEvent(updated.Id, callerUserId, previousName, updated.Name), ct);

        return updated;
    }

    public async Task<OrganizationRecord> SelectOrganizationAsync(Guid callerUserId, Guid organizationId, CancellationToken ct = default)
    {
        var org = await RequireActiveMembershipAsync(callerUserId, organizationId, ct);
        await _userRepository.SetCurrentOrganizationAsync(callerUserId, org.Id, ct);
        return org;
    }

    public async Task<OrgMemberRecord> InviteMemberAsync(
        Guid callerUserId,
        string memberEmail, string? role, CancellationToken ct = default)
    {
        var org = await RequireCurrentOrganizationAsync(callerUserId, ct);
        await RequireOrganizationAdminAsync(callerUserId, org.Id, ct);

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
        var user = await _userRepository.GetByAsync(new UserFilter { Id = callerUserId }, ct)
            ?? throw new InvalidOperationException("User not found.");
        await EnsureOwnedOrganizationAsync(user, ct);

        var member = await _organizationRepository.AcceptInviteAsync(memberId, callerUserId, callerEmail, ct);
        await _userRepository.SetCurrentOrganizationAsync(callerUserId, member.OrganizationId, ct);

        await _publisher.Publish(new OrganizationMemberInviteAcceptedEvent(
            member.OrganizationId,
            callerUserId,
            member.Id,
            member.Email,
            member.Role.ToString()), ct);
        return member;
    }

    public Task<bool> DeclineInviteAsync(
        Guid callerUserId,
        string callerEmail,
        Guid memberId,
        CancellationToken ct = default)
        => _organizationRepository.DeclineInviteAsync(memberId, callerUserId, callerEmail, ct);

    public async Task<bool> RemoveMemberAsync(
        Guid callerUserId,
        Guid memberId, CancellationToken ct = default)
    {
        var org = await RequireCurrentOrganizationAsync(callerUserId, ct);
        await RequireOrganizationAdminAsync(callerUserId, org.Id, ct);

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
        Guid callerUserId,
        string name, CancellationToken ct = default)
    {
        var org = await RequireCurrentOrganizationAsync(callerUserId, ct);
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
        Guid callerUserId,
        CancellationToken ct = default)
    {
        var org = await RequireCurrentOrganizationAsync(callerUserId, ct);
        return await _organizationRepository.ListMembersAsync(org.Id, ct);
    }

    private async Task<OrganizationRecord> RequireCurrentOrganizationAsync(Guid callerUserId, CancellationToken ct)
        => await GetCurrentOrganizationAsync(callerUserId, ct);

    private async Task<OrganizationRecord> RequireActiveMembershipAsync(Guid callerUserId, Guid organizationId, CancellationToken ct)
    {
        var org = await _organizationRepository.GetByAsync(new OrganizationFilter { Id = organizationId }, ct)
            ?? throw new InvalidOperationException("Organization not found.");
        if (!await HasActiveMembershipAsync(callerUserId, organizationId, ct))
            throw new InvalidOperationException("Organization not found.");

        return org;
    }

    private async Task RequireOrganizationAdminAsync(Guid callerUserId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == callerUserId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Only organization owners and admins may manage members.");
    }

    private async Task<bool> HasActiveMembershipAsync(Guid callerUserId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        return members.Any(member => member.UserId == callerUserId && member.Status == MemberStatus.Active);
    }

    private async Task<OrganizationRecord> EnsureOwnedOrganizationAsync(UserRecord user, CancellationToken ct)
    {
        var owned = await _organizationRepository.GetByAsync(new OrganizationFilter { OwnerUserId = user.Id }, ct);
        if (owned is null)
        {
            var organization = new OrganizationRecord
            {
                Id = Guid.NewGuid(),
                Name = IndividualOrganizationName(user),
                OwnerUserId = user.Id,
                Kind = OrganizationKind.Individual,
                CreatedAt = DateTime.UtcNow,
            };
            owned = await _organizationRepository.CreateAsync(
                organization,
                OrgMemberRecord.CreateOwner(organization.Id, user.Id, user.Email),
                ct);
            await _publisher.Publish(new OrganizationCreatedEvent(
                owned.Id,
                user.Id,
                owned.Name,
                user.Name), ct);
        }
        else
        {
            await _organizationRepository.EnsureOwnerMembershipAsync(owned.Id, user.Id, user.Email, ct);
        }

        await _workspaceRepository.EnsureOrganizationDefaultAsync(owned.Id, user.Id, ct);
        return owned;
    }

    private async Task<OrganizationRecord?> GetActiveMembershipOrganizationAsync(
        Guid callerUserId,
        Guid? organizationId,
        CancellationToken ct)
    {
        if (!organizationId.HasValue)
            return null;

        var org = await _organizationRepository.GetByAsync(new OrganizationFilter { Id = organizationId.Value }, ct);
        if (org is null || !await HasActiveMembershipAsync(callerUserId, org.Id, ct))
            return null;

        return org;
    }

    private static string IndividualOrganizationName(UserRecord user)
    {
        var displayName = !string.IsNullOrWhiteSpace(user.Name)
            ? user.Name.Trim()
            : user.Email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();

        return $"{(string.IsNullOrWhiteSpace(displayName) ? "User" : displayName)}'s Individual Org";
    }
}
