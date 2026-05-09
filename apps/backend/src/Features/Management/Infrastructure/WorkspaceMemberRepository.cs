namespace OffceOs.Infrastructure.Features.Management;

internal sealed class WorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public WorkspaceMemberRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<WorkspaceMemberRecord>> ListAsync(WorkspaceMemberFilter filter, CancellationToken ct = default)
    {
        var query = BuildQuery(filter);
        var entities = await query.OrderBy(m => m.CreatedAt).ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task<WorkspaceMemberRecord?> GetByAsync(WorkspaceMemberFilter filter, CancellationToken ct = default)
    {
        var entity = await BuildQuery(filter).FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<WorkspaceMemberRecord> UpsertAsync(WorkspaceMemberRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == record.WorkspaceId && m.UserId == record.UserId, ct);

        if (entity is null)
        {
            entity = ToEntity(record);
            _eaosDbContext.WorkspaceMembers.Add(entity);
        }
        else
        {
            entity.Role = record.Role.ToStorageString();
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(WorkspaceMemberFilter filter, CancellationToken ct = default)
    {
        var entities = await BuildQuery(filter).ToListAsync(ct);
        if (entities.Count == 0)
            return false;

        _eaosDbContext.WorkspaceMembers.RemoveRange(entities);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<WorkspaceMemberEntity> BuildQuery(WorkspaceMemberFilter filter)
    {
        var query = _eaosDbContext.WorkspaceMembers.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(m => m.Id == filter.Id.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(m => m.WorkspaceId == filter.WorkspaceId.Value);

        if (filter.UserId.HasValue)
            query = query.Where(m => m.UserId == filter.UserId.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(m => m.Workspace != null && m.Workspace.OrganizationId == filter.OrganizationId.Value);

        return query;
    }

    private static WorkspaceMemberRecord ToRecord(WorkspaceMemberEntity entity) => new()
    {
        Id = entity.Id,
        WorkspaceId = entity.WorkspaceId,
        UserId = entity.UserId,
        Role = entity.Role.ToWorkspaceRole(),
        CreatedAt = entity.CreatedAt,
    };

    private static WorkspaceMemberEntity ToEntity(WorkspaceMemberRecord record) => new()
    {
        Id = record.Id,
        WorkspaceId = record.WorkspaceId,
        UserId = record.UserId,
        Role = record.Role.ToStorageString(),
        CreatedAt = record.CreatedAt,
    };
}
