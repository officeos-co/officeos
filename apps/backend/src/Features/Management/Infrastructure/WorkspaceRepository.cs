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
            query = query.Where(w => w.UserId == filter.UserId.Value);

        var entities = await query
            .OrderBy(w => w.Name)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<WorkspaceRecord?> GetByAsync(WorkspaceFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Workspaces.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(w => w.Id == filter.Id.Value);

        if (filter.UserId.HasValue)
            query = query.Where(w => w.UserId == filter.UserId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<WorkspaceRecord> SaveAsync(WorkspaceRecord record, CancellationToken ct = default)
    {
        var duplicate = await _eaosDbContext.Workspaces.AsNoTracking().AnyAsync(
            w => w.UserId == record.UserId && w.Id != record.Id && w.Name == record.Name,
            ct);
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

        var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == record.UserId, ct);
        if (user is not null && user.CurrentWorkspaceId is null)
        {
            user.CurrentWorkspaceId = entity.Id;
            await _eaosDbContext.SaveChangesAsync(ct);
        }

        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);
        if (entity is null) return false;

        var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is not null && user.CurrentWorkspaceId == id)
            user.CurrentWorkspaceId = null;

        _eaosDbContext.Workspaces.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<WorkspaceRecord> EnsureDefaultAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return ToRecord(existing);

        var workspace = new WorkspaceEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Default",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.Workspaces.Add(workspace);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(workspace);
    }

    public async Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        WorkspaceEntity? workspace = null;
        if (user.CurrentWorkspaceId.HasValue)
        {
            workspace = await _eaosDbContext.Workspaces.AsNoTracking().FirstOrDefaultAsync(
                w => w.Id == user.CurrentWorkspaceId.Value && w.UserId == userId,
                ct);
        }

        workspace ??= await _eaosDbContext.Workspaces.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (workspace is null)
            return await CreateDefaultAndSetCurrentAsync(userId, ct);

        if (user.CurrentWorkspaceId != workspace.Id)
            await SetCurrentAsync(userId, workspace.Id, ct);

        return ToRecord(workspace);
    }

    public async Task SetCurrentAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var exists = await _eaosDbContext.Workspaces.AnyAsync(w => w.Id == workspaceId && w.UserId == userId, ct);
        if (!exists)
            throw new InvalidOperationException("Workspace not found.");

        var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        user.CurrentWorkspaceId = workspaceId;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private async Task<WorkspaceRecord> CreateDefaultAndSetCurrentAsync(Guid userId, CancellationToken ct)
    {
        var record = await SaveAsync(WorkspaceRecord.Create(userId, "Default"), ct);
        await SetCurrentAsync(userId, record.Id, ct);
        return record;
    }

    private static WorkspaceRecord ToRecord(WorkspaceEntity e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        Name = e.Name,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static WorkspaceEntity ToEntity(WorkspaceRecord r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Name = r.Name,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
