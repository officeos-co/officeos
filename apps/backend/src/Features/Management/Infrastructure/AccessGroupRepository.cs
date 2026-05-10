namespace OffceOs.Infrastructure.Features.Management;

internal sealed class AccessGroupRepository : IAccessGroupRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AccessGroupRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<AccessGroupRecord>> ListAsync(AccessGroupFilter filter, CancellationToken ct = default)
    {
        var entities = await BuildGroupQuery(filter)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<AccessGroupRecord?> GetByAsync(AccessGroupFilter filter, CancellationToken ct = default)
    {
        var entity = await BuildGroupQuery(filter).FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<AccessGroupRecord> SaveAsync(AccessGroupRecord record, CancellationToken ct = default)
    {
        var duplicate = await _eaosDbContext.AccessGroups.AsNoTracking()
            .AnyAsync(g => g.Id != record.Id && g.OrganizationId == record.OrganizationId && g.Name == record.Name, ct);
        if (duplicate)
            throw new InvalidOperationException("An access group with that name already exists.");

        var entity = await _eaosDbContext.AccessGroups.FirstOrDefaultAsync(g => g.Id == record.Id, ct);
        if (entity is null)
        {
            entity = ToEntity(record);
            _eaosDbContext.AccessGroups.Add(entity);
        }
        else
        {
            entity.Name = record.Name;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(AccessGroupFilter filter, CancellationToken ct = default)
    {
        var entities = await BuildGroupQuery(filter).ToListAsync(ct);
        if (entities.Count == 0)
            return false;

        _eaosDbContext.AccessGroups.RemoveRange(entities);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AccessGroupMemberRecord>> ListMembersAsync(AccessGroupFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AccessGroupMembers.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(m => m.AccessGroupId == filter.Id.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(m => m.AccessGroup != null && m.AccessGroup.OrganizationId == filter.OrganizationId.Value);

        if (filter.UserId.HasValue)
            query = query.Where(m => m.UserId == filter.UserId.Value);

        var entities = await query.OrderBy(m => m.CreatedAt).ToListAsync(ct);
        return entities.Select(ToMemberRecord).ToList();
    }

    public async Task<AccessGroupMemberRecord> AddMemberAsync(Guid accessGroupId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AccessGroupMembers
            .FirstOrDefaultAsync(m => m.AccessGroupId == accessGroupId && m.UserId == userId, ct);
        if (existing is not null)
            return ToMemberRecord(existing);

        var entity = new AccessGroupMemberEntity
        {
            Id = Guid.NewGuid(),
            AccessGroupId = accessGroupId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.AccessGroupMembers.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToMemberRecord(entity);
    }

    public async Task<bool> RemoveMemberAsync(Guid accessGroupId, Guid userId, CancellationToken ct = default)
    {
        var deleted = await _eaosDbContext.AccessGroupMembers
            .Where(m => m.AccessGroupId == accessGroupId && m.UserId == userId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public async Task<IReadOnlyList<AccessGroupWorkspaceGrantRecord>> ListWorkspaceGrantsAsync(AccessGroupFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AccessGroupWorkspaceGrants.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(g => g.AccessGroupId == filter.Id.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(g => g.AccessGroup != null && g.AccessGroup.OrganizationId == filter.OrganizationId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(g => g.WorkspaceId == filter.WorkspaceId.Value);

        if (filter.UserId.HasValue)
        {
            query = query.Where(g => _eaosDbContext.AccessGroupMembers
                .Any(m => m.AccessGroupId == g.AccessGroupId && m.UserId == filter.UserId.Value));
        }

        var entities = await query.OrderBy(g => g.CreatedAt).ToListAsync(ct);
        return entities.Select(ToGrantRecord).ToList();
    }

    public async Task<AccessGroupWorkspaceGrantRecord> UpsertWorkspaceGrantAsync(AccessGroupWorkspaceGrantRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AccessGroupWorkspaceGrants
            .FirstOrDefaultAsync(g => g.AccessGroupId == record.AccessGroupId && g.WorkspaceId == record.WorkspaceId, ct);

        if (entity is null)
        {
            entity = new AccessGroupWorkspaceGrantEntity
            {
                Id = record.Id,
                AccessGroupId = record.AccessGroupId,
                WorkspaceId = record.WorkspaceId,
                Role = record.Role.ToStorageString(),
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
            };
            _eaosDbContext.AccessGroupWorkspaceGrants.Add(entity);
        }
        else
        {
            entity.Role = record.Role.ToStorageString();
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToGrantRecord(entity);
    }

    public async Task<bool> DeleteWorkspaceGrantAsync(Guid accessGroupId, Guid workspaceId, CancellationToken ct = default)
    {
        var deleted = await _eaosDbContext.AccessGroupWorkspaceGrants
            .Where(g => g.AccessGroupId == accessGroupId && g.WorkspaceId == workspaceId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    private IQueryable<AccessGroupEntity> BuildGroupQuery(AccessGroupFilter filter)
    {
        var query = _eaosDbContext.AccessGroups.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(g => g.Id == filter.Id.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(g => g.OrganizationId == filter.OrganizationId.Value);

        if (filter.UserId.HasValue)
        {
            query = query.Where(g => _eaosDbContext.AccessGroupMembers
                .Any(m => m.AccessGroupId == g.Id && m.UserId == filter.UserId.Value));
        }

        if (filter.WorkspaceId.HasValue)
        {
            query = query.Where(g => _eaosDbContext.AccessGroupWorkspaceGrants
                .Any(grant => grant.AccessGroupId == g.Id && grant.WorkspaceId == filter.WorkspaceId.Value));
        }

        return query;
    }

    private static AccessGroupRecord ToRecord(AccessGroupEntity entity) => new()
    {
        Id = entity.Id,
        OrganizationId = entity.OrganizationId,
        Name = entity.Name,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    private static AccessGroupEntity ToEntity(AccessGroupRecord record) => new()
    {
        Id = record.Id,
        OrganizationId = record.OrganizationId,
        Name = record.Name,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
    };

    private static AccessGroupMemberRecord ToMemberRecord(AccessGroupMemberEntity entity) => new()
    {
        Id = entity.Id,
        AccessGroupId = entity.AccessGroupId,
        UserId = entity.UserId,
        CreatedAt = entity.CreatedAt,
    };

    private static AccessGroupWorkspaceGrantRecord ToGrantRecord(AccessGroupWorkspaceGrantEntity entity) => new()
    {
        Id = entity.Id,
        AccessGroupId = entity.AccessGroupId,
        WorkspaceId = entity.WorkspaceId,
        Role = entity.Role.ToWorkspaceRole(),
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
