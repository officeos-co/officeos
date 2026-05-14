namespace OffceOs.Infrastructure.Features.Management;

internal sealed class WorkspaceRepository : IWorkspaceRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public WorkspaceRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<WorkspaceRecord>> ListAsync(WorkspaceFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Workspaces.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(w => w.Id == filter.Id.Value);

        if (filter.UserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.UserId.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.OwnerUserId.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(w => w.OrganizationId == filter.OrganizationId.Value);

        if (filter.OwnerKind.HasValue)
        {
            var ownerKind = filter.OwnerKind.Value.ToStorageString();
            query = query.Where(w => w.OwnerKind == ownerKind);
        }

        if (filter.IsDefault.HasValue)
            query = query.Where(w => w.IsDefault == filter.IsDefault.Value);

        var entities = await query
            .OrderBy(w => w.Name)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(entity => ToRecord(entity)).ToList();
    }

    public async Task<IReadOnlyList<WorkspaceRecord>> ListAccessibleAsync(Guid userId, CancellationToken ct = default)
    {
        var direct = await _eaosDbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking(),
                m => m.WorkspaceId,
                w => w.Id,
                (m, w) => new { Member = m, Workspace = w })
            .ToListAsync(ct);

        var granted = await _eaosDbContext.OrgMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == MemberStatus.Active.ToStorageString())
            .Join(
                _eaosDbContext.WorkspaceOrganizationGrants.AsNoTracking(),
                m => m.OrganizationId,
                g => g.OrganizationId,
                (m, g) => g)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking(),
                g => g.WorkspaceId,
                w => w.Id,
                (g, w) => new { Grant = g, Workspace = w })
            .ToListAsync(ct);

        var groupGranted = await _eaosDbContext.AccessGroupMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _eaosDbContext.AccessGroupWorkspaceGrants.AsNoTracking(),
                m => m.AccessGroupId,
                g => g.AccessGroupId,
                (m, g) => g)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking(),
                g => g.WorkspaceId,
                w => w.Id,
                (g, w) => new { Grant = g, Workspace = w })
            .ToListAsync(ct);

        var records = direct
            .Select(row => ToRecord(row.Workspace, row.Member.Role.ToWorkspaceRole()))
            .Concat(granted.Select(row => ToRecord(row.Workspace, row.Grant.MaxRole.ToWorkspaceRole())))
            .Concat(groupGranted.Select(row => ToRecord(row.Workspace, row.Grant.Role.ToWorkspaceRole())))
            .GroupBy(w => w.Id)
            .Select(g => g.OrderByDescending(w => RoleRank(w.Role ?? WorkspaceRole.Viewer)).First())
            .OrderBy(w => w.OwnerKind)
            .ThenByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .ToList();

        return records;
    }

    public async Task<WorkspaceRecord?> GetByAsync(WorkspaceFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Workspaces.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(w => w.Id == filter.Id.Value);

        if (filter.UserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.UserId.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.OwnerUserId.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(w => w.OrganizationId == filter.OrganizationId.Value);

        if (filter.OwnerKind.HasValue)
        {
            var ownerKind = filter.OwnerKind.Value.ToStorageString();
            query = query.Where(w => w.OwnerKind == ownerKind);
        }

        if (filter.IsDefault.HasValue)
            query = query.Where(w => w.IsDefault == filter.IsDefault.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<WorkspaceRecord?> GetAccessibleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var direct = await _eaosDbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking(),
                m => m.WorkspaceId,
                w => w.Id,
                (m, w) => new { Member = m, Workspace = w })
            .FirstOrDefaultAsync(ct);

        if (direct is not null)
            return ToRecord(direct.Workspace, direct.Member.Role.ToWorkspaceRole());

        var groupGrants = await _eaosDbContext.AccessGroupMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _eaosDbContext.AccessGroupWorkspaceGrants.AsNoTracking().Where(g => g.WorkspaceId == workspaceId),
                m => m.AccessGroupId,
                g => g.AccessGroupId,
                (m, g) => g)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking(),
                g => g.WorkspaceId,
                w => w.Id,
                (g, w) => new { Grant = g, Workspace = w })
            .ToListAsync(ct);

        var groupGrant = groupGrants
            .OrderByDescending(row => RoleRank(row.Grant.Role.ToWorkspaceRole()))
            .FirstOrDefault();

        if (groupGrant is not null)
            return ToRecord(groupGrant.Workspace, groupGrant.Grant.Role.ToWorkspaceRole());

        var grant = await _eaosDbContext.OrgMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == MemberStatus.Active.ToStorageString())
            .Join(
                _eaosDbContext.WorkspaceOrganizationGrants.AsNoTracking().Where(g => g.WorkspaceId == workspaceId),
                m => m.OrganizationId,
                g => g.OrganizationId,
                (m, g) => g)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking(),
                g => g.WorkspaceId,
                w => w.Id,
                (g, w) => new { Grant = g, Workspace = w })
            .FirstOrDefaultAsync(ct);

        return grant is null ? null : ToRecord(grant.Workspace, grant.Grant.MaxRole.ToWorkspaceRole());
    }

    public async Task<WorkspaceRecord> SaveAsync(WorkspaceRecord record, CancellationToken ct = default)
    {
        var duplicateQuery = _eaosDbContext.Workspaces.AsNoTracking()
            .Where(w => w.Id != record.Id && w.Name == record.Name);

        duplicateQuery = record.OwnerKind switch
        {
            WorkspaceOwnerKind.Personal => duplicateQuery.Where(w => w.OwnerKind == WorkspaceOwnerKind.Personal.ToStorageString()
                && w.OwnerUserId == record.OwnerUserId),
            WorkspaceOwnerKind.Organization => duplicateQuery.Where(w => w.OwnerKind == WorkspaceOwnerKind.Organization.ToStorageString()
                && w.OrganizationId == record.OrganizationId),
            _ => duplicateQuery,
        };

        var duplicate = await duplicateQuery.AnyAsync(ct);
        if (duplicate)
            throw new InvalidOperationException("A workspace with that name already exists.");

        var entity = await _eaosDbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == record.Id, ct);
        if (entity is null)
        {
            entity = ToEntity(record);
            _eaosDbContext.Workspaces.Add(entity);
        }
        else
        {
            entity.Name = record.Name;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);

        if (record.OwnerKind == WorkspaceOwnerKind.Personal && record.OwnerUserId.HasValue)
        {
            var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == record.OwnerUserId.Value, ct);
            if (user is not null && user.CurrentWorkspaceId is null)
            {
                user.CurrentWorkspaceId = entity.Id;
                await _eaosDbContext.SaveChangesAsync(ct);
            }
        }

        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entity is null) return false;

        var usersWithCurrentWorkspace = await _eaosDbContext.Users
            .Where(u => u.CurrentWorkspaceId == id)
            .ToListAsync(ct);
        foreach (var user in usersWithCurrentWorkspace)
            user.CurrentWorkspaceId = null;

        _eaosDbContext.Workspaces.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<WorkspaceRecord> EnsurePersonalDefaultAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.OwnerKind == WorkspaceOwnerKind.Personal.ToStorageString() && w.OwnerUserId == userId)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            await EnsureMembershipAsync(existing.Id, userId, WorkspaceRole.Admin, ct);
            return ToRecord(existing, WorkspaceRole.Admin);
        }

        var workspace = new WorkspaceEntity
        {
            Id = Guid.NewGuid(),
            OwnerKind = WorkspaceOwnerKind.Personal.ToStorageString(),
            OwnerUserId = userId,
            Name = "Default",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.Workspaces.Add(workspace);
        await _eaosDbContext.SaveChangesAsync(ct);
        await EnsureMembershipAsync(workspace.Id, userId, WorkspaceRole.Admin, ct);
        return ToRecord(workspace, WorkspaceRole.Admin);
    }

    public async Task<WorkspaceRecord> EnsureOrganizationDefaultAsync(Guid organizationId, Guid ownerUserId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.OwnerKind == WorkspaceOwnerKind.Organization.ToStorageString()
                && w.OrganizationId == organizationId
                && w.IsDefault)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            await EnsureMembershipAsync(existing.Id, ownerUserId, WorkspaceRole.Admin, ct);
            return ToRecord(existing, WorkspaceRole.Admin);
        }

        var organization = await _eaosDbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        var workspace = new WorkspaceEntity
        {
            Id = Guid.NewGuid(),
            OwnerKind = WorkspaceOwnerKind.Organization.ToStorageString(),
            OrganizationId = organizationId,
            Name = organization is null ? "Organization" : organization.Name,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.Workspaces.Add(workspace);
        await _eaosDbContext.SaveChangesAsync(ct);
        await EnsureMembershipAsync(workspace.Id, ownerUserId, WorkspaceRole.Admin, ct);
        return ToRecord(workspace, WorkspaceRole.Admin);
    }

    public async Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        WorkspaceEntity? workspace = null;
        if (user.CurrentWorkspaceId.HasValue)
        {
            var accessible = await GetAccessibleAsync(userId, user.CurrentWorkspaceId.Value, ct);
            if (accessible is not null)
                return accessible;
        }

        workspace ??= await _eaosDbContext.Workspaces.AsNoTracking()
            .Where(w => w.OwnerKind == WorkspaceOwnerKind.Personal.ToStorageString() && w.OwnerUserId == userId)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (workspace is null)
            return await CreateDefaultAndSetCurrentAsync(userId, ct);

        if (user.CurrentWorkspaceId != workspace.Id)
            await SetCurrentAsync(userId, workspace.Id, ct);

        await EnsureMembershipAsync(workspace.Id, userId, WorkspaceRole.Admin, ct);
        return ToRecord(workspace, WorkspaceRole.Admin);
    }

    public async Task SetCurrentAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var accessible = await GetAccessibleAsync(userId, workspaceId, ct);
        if (accessible is null)
            throw new InvalidOperationException("Workspace not found.");

        var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        user.CurrentWorkspaceId = workspaceId;
        if (accessible.OrganizationId.HasValue)
            user.CurrentOrganizationId = accessible.OrganizationId.Value;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<WorkspaceOrganizationGrantRecord> UpsertOrganizationGrantAsync(
        WorkspaceOrganizationGrantRecord record,
        CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.WorkspaceOrganizationGrants
            .FirstOrDefaultAsync(g => g.WorkspaceId == record.WorkspaceId && g.OrganizationId == record.OrganizationId, ct);

        if (entity is null)
        {
            entity = new WorkspaceOrganizationGrantEntity
            {
                Id = record.Id,
                WorkspaceId = record.WorkspaceId,
                OrganizationId = record.OrganizationId,
                MaxRole = record.MaxRole.ToStorageString(),
                CreatedAt = record.CreatedAt,
            };
            _eaosDbContext.WorkspaceOrganizationGrants.Add(entity);
        }
        else
        {
            entity.MaxRole = record.MaxRole.ToStorageString();
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return new WorkspaceOrganizationGrantRecord
        {
            Id = entity.Id,
            WorkspaceId = entity.WorkspaceId,
            OrganizationId = entity.OrganizationId,
            MaxRole = entity.MaxRole.ToWorkspaceRole(),
            CreatedAt = entity.CreatedAt,
        };
    }

    public async Task<bool> DeleteOrganizationGrantAsync(Guid workspaceId, Guid organizationId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.WorkspaceOrganizationGrants
            .FirstOrDefaultAsync(g => g.WorkspaceId == workspaceId && g.OrganizationId == organizationId, ct);
        if (entity is null)
            return false;

        _eaosDbContext.WorkspaceOrganizationGrants.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    private async Task<WorkspaceRecord> CreateDefaultAndSetCurrentAsync(Guid userId, CancellationToken ct)
    {
        var record = await SaveAsync(WorkspaceRecord.CreatePersonal(userId, "Default", isDefault: true), ct);
        await EnsureMembershipAsync(record.Id, userId, WorkspaceRole.Admin, ct);
        await SetCurrentAsync(userId, record.Id, ct);
        return record;
    }

    private async Task EnsureMembershipAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct)
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

    private static WorkspaceRecord ToRecord(WorkspaceEntity e, WorkspaceRole? role = null) => new()
    {
        Id = e.Id,
        OwnerKind = e.OwnerKind.ToWorkspaceOwnerKind(),
        OwnerUserId = e.OwnerUserId,
        OrganizationId = e.OrganizationId,
        Name = e.Name,
        IsDefault = e.IsDefault,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Role = role,
    };

    private static WorkspaceEntity ToEntity(WorkspaceRecord r) => new()
    {
        Id = r.Id,
        OwnerKind = r.OwnerKind.ToStorageString(),
        OwnerUserId = r.OwnerUserId,
        OrganizationId = r.OrganizationId,
        Name = r.Name,
        IsDefault = r.IsDefault,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
