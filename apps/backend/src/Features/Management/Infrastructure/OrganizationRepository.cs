namespace OffceOs.Infrastructure.Features.Management;

internal sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrganizationRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default)
    {
        var owned = await _eaosDbContext.Organizations
            .FirstOrDefaultAsync(o => o.OwnerUserId == ownerUserId, ct);
        if (owned is not null) return ToOrganizationRecord(owned);

        var orgEntity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(ownerName) ? "My Organization" : $"{ownerName}'s Organization",
            OwnerUserId = ownerUserId,
            CreatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.Organizations.Add(orgEntity);

        _eaosDbContext.OrgMembers.Add(new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgEntity.Id,
            UserId = ownerUserId,
            Email = ownerEmail,
            Role = OrgRole.Owner.ToStorageString(),
            Status = MemberStatus.Active.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        await _eaosDbContext.SaveChangesAsync(ct);
        await EnsureOrganizationDefaultWorkspaceAsync(orgEntity.Id, ownerUserId, ct);
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

    public async Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.OrgMembers
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToOrgMemberRecord).ToList();
    }

    public async Task<OrgMemberRecord> AddMemberAsync(OrgMemberRecord member, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.OrgMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == member.OrganizationId && m.Email == member.Email, ct);
        if (existing is not null)
        {
            await EnsureDefaultWorkspaceMembershipAsync(existing.OrganizationId, existing.UserId, ct);
            return ToOrgMemberRecord(existing);
        }

        var user = await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == member.Email, ct);
        if (user is not null)
        {
            member.UserId = user.Id;
            member.Status = MemberStatus.Active;
        }

        var entity = ToOrgMemberEntity(member);
        _eaosDbContext.OrgMembers.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        await EnsureDefaultWorkspaceMembershipAsync(entity.OrganizationId, entity.UserId, ct);
        return ToOrgMemberRecord(entity);
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

        await EnsureWorkspaceMembershipAsync(workspace.Id, ownerUserId, WorkspaceRole.Owner, ct);
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

        await EnsureWorkspaceMembershipAsync(workspace.Id, userId.Value, WorkspaceRole.Editor, ct);
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
        WorkspaceRole.Owner => 4,
        WorkspaceRole.Admin => 3,
        WorkspaceRole.Editor => 2,
        WorkspaceRole.Viewer => 1,
        _ => 0,
    };

    // ── Mapping ──────────────────────────────────────────────────────

    private static OrganizationRecord ToOrganizationRecord(OrganizationEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        OwnerUserId = e.OwnerUserId,
        CreatedAt = e.CreatedAt,
    };

    private static OrganizationEntity ToOrganizationEntity(OrganizationRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        OwnerUserId = r.OwnerUserId,
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
        Email = r.Email,
        Role = r.Role.ToStorageString(),
        Status = r.Status.ToStorageString(),
        CreatedAt = r.CreatedAt,
    };
}
