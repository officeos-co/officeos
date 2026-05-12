namespace OffceOs.Infrastructure.Features.Management;

internal sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrganizationRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<OrganizationRecord> CreateAsync(
        OrganizationRecord organization,
        OrgMemberRecord ownerMember,
        CancellationToken ct = default)
    {
        var owned = await _eaosDbContext.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.OwnerUserId == organization.OwnerUserId, ct);
        if (owned)
            throw new InvalidOperationException("User already owns an organization.");

        var orgEntity = ToOrganizationEntity(organization);
        _eaosDbContext.Organizations.Add(orgEntity);
        _eaosDbContext.OrgMembers.Add(ToOrgMemberEntity(ownerMember));
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToOrganizationRecord(orgEntity);
    }

    public async Task<OrganizationRecord?> GetByAsync(OrganizationFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Organizations.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(o => o.Id == filter.Id.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(o => o.OwnerUserId == filter.OwnerUserId.Value);

        if (!string.IsNullOrEmpty(filter.Name))
            query = query.Where(o => o.Name == filter.Name);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToOrganizationRecord(entity);
    }

    public async Task<IReadOnlyList<OrganizationRecord>> ListForMemberAsync(Guid userId, CancellationToken ct = default)
    {
        var active = MemberStatus.Active.ToStorageString();
        var rows = await _eaosDbContext.OrgMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId && member.Status == active)
            .Join(
                _eaosDbContext.Organizations.AsNoTracking(),
                member => member.OrganizationId,
                organization => organization.Id,
                (member, organization) => organization)
            .OrderBy(organization => organization.Name)
            .ThenBy(organization => organization.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToOrganizationRecord).ToList();
    }

    public async Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.OrgMembers
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToOrgMemberRecord).ToList();
    }

    public async Task<OrgMemberRecord> EnsureOwnerMembershipAsync(
        Guid organizationId,
        Guid userId,
        string email,
        CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var entity = await _eaosDbContext.OrgMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);

        if (entity is null)
        {
            entity = await _eaosDbContext.OrgMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.Email == normalizedEmail, ct);
        }

        if (entity is null)
        {
            entity = ToOrgMemberEntity(OrgMemberRecord.CreateOwner(organizationId, userId, normalizedEmail));
            _eaosDbContext.OrgMembers.Add(entity);
        }
        else
        {
            entity.UserId = userId;
            entity.Email = normalizedEmail;
            entity.Role = OrgRole.Owner.ToStorageString();
            entity.Status = MemberStatus.Active.ToStorageString();
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        await EnsureDefaultWorkspaceMembershipAsync(organizationId, userId, ct);
        return ToOrgMemberRecord(entity);
    }

    public async Task<OrganizationRecord> SaveAsync(OrganizationRecord organization, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organization.Id, ct);
        if (entity is null)
        {
            entity = ToOrganizationEntity(organization);
            _eaosDbContext.Organizations.Add(entity);
        }
        else
        {
            entity.Name = organization.Name;
            entity.Kind = organization.Kind.ToStorageString();
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToOrganizationRecord(entity);
    }

    public async Task<IReadOnlyList<OrganizationInviteRecord>> ListPendingInvitesForEmailAsync(
        string email,
        CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return [];

        var invited = MemberStatus.Invited.ToStorageString();
        var rows = await _eaosDbContext.OrgMembers
            .AsNoTracking()
            .Where(member => member.Email == normalizedEmail
                && member.Status == invited)
            .Join(
                _eaosDbContext.Organizations.AsNoTracking(),
                member => member.OrganizationId,
                organization => organization.Id,
                (member, organization) => new { Member = member, Organization = organization })
            .OrderBy(row => row.Member.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(row => new OrganizationInviteRecord
        {
            Id = row.Member.Id,
            OrganizationId = row.Member.OrganizationId,
            OrganizationName = row.Organization.Name,
            Email = row.Member.Email,
            Role = row.Member.Role.ToOrgRole(),
            CreatedAt = row.Member.CreatedAt,
        }).ToList();
    }

    public async Task<OrgMemberRecord> AddMemberAsync(OrgMemberRecord member, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(member.Email);
        var existing = await _eaosDbContext.OrgMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == member.OrganizationId && m.Email == normalizedEmail, ct);
        if (existing is not null)
        {
            var invited = MemberStatus.Invited.ToStorageString();
            if (existing.Status == invited && !existing.UserId.HasValue)
            {
                var invitedUser = await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
                if (invitedUser is not null)
                {
                    existing.UserId = invitedUser.Id;
                    await _eaosDbContext.SaveChangesAsync(ct);
                }
            }
            else if (existing.Status == MemberStatus.Active.ToStorageString())
            {
                await EnsureDefaultWorkspaceMembershipAsync(existing.OrganizationId, existing.UserId, ct);
            }

            return ToOrgMemberRecord(existing);
        }

        var user = await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (user is not null)
        {
            member.UserId = user.Id;
        }

        var entity = ToOrgMemberEntity(member);
        _eaosDbContext.OrgMembers.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToOrgMemberRecord(entity);
    }

    public async Task<OrgMemberRecord> AcceptInviteAsync(Guid memberId, Guid userId, string email, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var entity = await _eaosDbContext.OrgMembers.FirstOrDefaultAsync(m => m.Id == memberId, ct)
            ?? throw new InvalidOperationException("Invite not found.");

        if (entity.Email != normalizedEmail)
            throw new InvalidOperationException("Invite not found.");

        if (entity.UserId.HasValue && entity.UserId != userId)
            throw new InvalidOperationException("Invite not found.");

        if (entity.Status != MemberStatus.Invited.ToStorageString())
            throw new InvalidOperationException("Invite not found.");

        entity.UserId = userId;
        entity.Status = MemberStatus.Active.ToStorageString();
        await _eaosDbContext.SaveChangesAsync(ct);
        await EnsureDefaultWorkspaceMembershipAsync(entity.OrganizationId, userId, ct);
        return ToOrgMemberRecord(entity);
    }

    public async Task<bool> DeclineInviteAsync(Guid memberId, Guid userId, string email, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var entity = await _eaosDbContext.OrgMembers.FirstOrDefaultAsync(m => m.Id == memberId, ct);
        if (entity is null)
            return false;

        if (entity.Email != normalizedEmail)
            throw new InvalidOperationException("Invite not found.");

        if (entity.UserId.HasValue && entity.UserId != userId)
            throw new InvalidOperationException("Invite not found.");

        if (entity.Status != MemberStatus.Invited.ToStorageString())
            throw new InvalidOperationException("Invite not found.");

        _eaosDbContext.OrgMembers.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.OrgMembers.FirstOrDefaultAsync(x => x.Id == memberId, ct);
        if (entity is null) return false;
        _eaosDbContext.OrgMembers.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<OrganizationRecord> RenameAsync(
        Guid organizationId, string name, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new InvalidOperationException($"organization {organizationId} not found");
        entity.Name = name;
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToOrganizationRecord(entity);
    }

    private async Task EnsureOrganizationDefaultWorkspaceAsync(Guid organizationId, Guid ownerUserId, CancellationToken ct)
    {
        var workspace = await _eaosDbContext.Workspaces
            .FirstOrDefaultAsync(w => w.OrganizationId == organizationId && w.IsDefault, ct);

        if (workspace is null)
        {
            var organization = await _eaosDbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId, ct);
            workspace = new WorkspaceEntity
            {
                Id = Guid.NewGuid(),
                OwnerKind = WorkspaceOwnerKind.Organization.ToStorageString(),
                OrganizationId = organizationId,
                Name = organization?.Name ?? "Organization",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _eaosDbContext.Workspaces.Add(workspace);
            await _eaosDbContext.SaveChangesAsync(ct);
        }

        await EnsureWorkspaceMembershipAsync(workspace.Id, ownerUserId, WorkspaceRole.Admin, ct);
    }

    private async Task EnsureDefaultWorkspaceMembershipAsync(Guid organizationId, Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue)
            return;

        var workspace = await _eaosDbContext.Workspaces
            .FirstOrDefaultAsync(w => w.OrganizationId == organizationId && w.IsDefault, ct);

        if (workspace is null)
        {
            var organization = await _eaosDbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId, ct);
            if (organization is null)
                return;

            await EnsureOrganizationDefaultWorkspaceAsync(organizationId, organization.OwnerUserId, ct);
            workspace = await _eaosDbContext.Workspaces
                .FirstOrDefaultAsync(w => w.OrganizationId == organizationId && w.IsDefault, ct);
        }

        if (workspace is null)
            return;

        var member = await _eaosDbContext.OrgMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId
                && m.UserId == userId.Value
                && m.Status == MemberStatus.Active.ToStorageString(), ct);

        await EnsureWorkspaceMembershipAsync(
            workspace.Id,
            userId.Value,
            ToWorkspaceRole(member?.Role.ToOrgRole() ?? OrgRole.Editor),
            ct);
    }

    private async Task EnsureWorkspaceMembershipAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct)
    {
        var existing = await _eaosDbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (existing is null)
        {
            _eaosDbContext.WorkspaceMembers.Add(new WorkspaceMemberEntity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                UserId = userId,
                Role = role.ToStorageString(),
                CreatedAt = DateTime.UtcNow,
            });
        }
        else if (RoleRank(existing.Role.ToWorkspaceRole()) < RoleRank(role))
        {
            existing.Role = role.ToStorageString();
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static int RoleRank(WorkspaceRole role) => role switch
    {
        WorkspaceRole.Admin => 3,
        WorkspaceRole.Editor => 2,
        WorkspaceRole.Viewer => 1,
        _ => 0,
    };

    private static WorkspaceRole ToWorkspaceRole(OrgRole role) => role switch
    {
        OrgRole.Owner => WorkspaceRole.Admin,
        OrgRole.Admin => WorkspaceRole.Admin,
        OrgRole.Editor => WorkspaceRole.Editor,
        OrgRole.Viewer => WorkspaceRole.Viewer,
        _ => WorkspaceRole.Editor,
    };

    // ── Mapping ──────────────────────────────────────────────────────

    private static OrganizationRecord ToOrganizationRecord(OrganizationEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        OwnerUserId = e.OwnerUserId,
        Kind = e.Kind.ToOrganizationKind(),
        CreatedAt = e.CreatedAt,
    };

    private static OrganizationEntity ToOrganizationEntity(OrganizationRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        OwnerUserId = r.OwnerUserId,
        Kind = r.Kind.ToStorageString(),
        CreatedAt = r.CreatedAt,
    };

    private static OrgMemberRecord ToOrgMemberRecord(OrgMemberEntity e) => new()
    {
        Id = e.Id,
        OrganizationId = e.OrganizationId,
        UserId = e.UserId,
        Email = e.Email,
        Role = e.Role.ToOrgRole(),
        Status = e.Status.ToMemberStatus(),
        CreatedAt = e.CreatedAt,
    };

    private static OrgMemberEntity ToOrgMemberEntity(OrgMemberRecord r) => new()
    {
        Id = r.Id,
        OrganizationId = r.OrganizationId,
        UserId = r.UserId,
        Email = NormalizeEmail(r.Email),
        Role = r.Role.ToStorageString(),
        Status = r.Status.ToStorageString(),
        CreatedAt = r.CreatedAt,
    };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
