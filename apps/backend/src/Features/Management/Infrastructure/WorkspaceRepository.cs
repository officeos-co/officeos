using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Management;
namespace OffceOs.Infrastructure.Features.Management;

internal sealed class WorkspaceRepository : IWorkspaceRepository
{
    private static readonly string PersonalOwnerKind = WorkspaceOwnerKind.Personal.ToStorageString();
    private readonly EaosDbContext _eaosDbContext;

    public WorkspaceRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<WorkspaceRecord>> ListAsync(WorkspaceFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Workspaces.AsNoTracking()
            .Where(w => w.OwnerKind == PersonalOwnerKind)
            .AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(w => w.Id == filter.Id.Value);

        if (filter.UserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.UserId.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.OwnerUserId.Value);

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
        var rows = await _eaosDbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking().Where(w => w.OwnerKind == PersonalOwnerKind),
                m => m.WorkspaceId,
                w => w.Id,
                (m, w) => new { Member = m, Workspace = w })
            .OrderByDescending(row => row.Workspace.IsDefault)
            .ThenBy(row => row.Workspace.Name)
            .ThenBy(row => row.Workspace.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(row => ToRecord(row.Workspace, row.Member.Role.ToWorkspaceRole())).ToList();
    }

    public async Task<WorkspaceRecord?> GetByAsync(WorkspaceFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Workspaces.AsNoTracking()
            .Where(w => w.OwnerKind == PersonalOwnerKind)
            .AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(w => w.Id == filter.Id.Value);

        if (filter.UserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.UserId.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(w => w.OwnerUserId == filter.OwnerUserId.Value);

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
        var row = await _eaosDbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .Join(
                _eaosDbContext.Workspaces.AsNoTracking().Where(w => w.OwnerKind == PersonalOwnerKind),
                m => m.WorkspaceId,
                w => w.Id,
                (m, w) => new { Member = m, Workspace = w })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToRecord(row.Workspace, row.Member.Role.ToWorkspaceRole());
    }

    public async Task<WorkspaceRecord> SaveAsync(WorkspaceRecord record, CancellationToken ct = default)
    {
        var duplicate = await _eaosDbContext.Workspaces.AsNoTracking()
            .AnyAsync(w => w.Id != record.Id
                && w.OwnerKind == PersonalOwnerKind
                && w.OwnerUserId == record.OwnerUserId
                && w.Name == record.Name, ct);
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

        if (record.OwnerUserId.HasValue)
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
            .Where(w => w.OwnerKind == PersonalOwnerKind && w.OwnerUserId == userId)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            await EnsureMembershipAsync(existing.Id, userId, WorkspaceRole.Owner, ct);
            return ToRecord(existing, WorkspaceRole.Owner);
        }

        var workspace = new WorkspaceEntity
        {
            Id = Guid.NewGuid(),
            OwnerKind = PersonalOwnerKind,
            OwnerUserId = userId,
            Name = "Default",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.Workspaces.Add(workspace);
        await _eaosDbContext.SaveChangesAsync(ct);
        await EnsureMembershipAsync(workspace.Id, userId, WorkspaceRole.Owner, ct);
        await SetCurrentAsync(userId, workspace.Id, ct);
        return ToRecord(workspace, WorkspaceRole.Owner);
    }

    public async Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        if (user.CurrentWorkspaceId.HasValue)
        {
            var accessible = await GetAccessibleAsync(userId, user.CurrentWorkspaceId.Value, ct);
            if (accessible is not null)
                return accessible;
        }

        var workspace = await _eaosDbContext.Workspaces.AsNoTracking()
            .Where(w => w.OwnerKind == PersonalOwnerKind && w.OwnerUserId == userId)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (workspace is null)
            return await EnsurePersonalDefaultAsync(userId, ct);

        await EnsureMembershipAsync(workspace.Id, userId, WorkspaceRole.Owner, ct);
        await SetCurrentAsync(userId, workspace.Id, ct);
        return ToRecord(workspace, WorkspaceRole.Owner);
    }

    public async Task SetCurrentAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var accessible = await GetAccessibleAsync(userId, workspaceId, ct);
        if (accessible is null)
            throw new InvalidOperationException("Workspace not found.");

        var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        user.CurrentWorkspaceId = workspaceId;
        await _eaosDbContext.SaveChangesAsync(ct);
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
        WorkspaceRole.Owner => 4,
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
        Name = r.Name,
        IsDefault = r.IsDefault,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
