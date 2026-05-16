using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Browser;
using OffceOs.Domain.Features.Agents;
namespace OffceOs.Infrastructure.Features.Browser;

internal sealed class BrowserResourceRepository : IBrowserResourceRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public BrowserResourceRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<BrowserResourceRecord>> ListAsync(BrowserResourceFilter filter, CancellationToken ct = default)
    {
        var entities = await ApplyFilter(_eaosDbContext.BrowserResources.AsNoTracking(), filter)
            .OrderByDescending(resource => resource.UpdatedAt)
            .ToListAsync(ct);
        return entities.Select(ToBrowserRecord).ToList();
    }

    public async Task<BrowserResourceRecord?> GetByAsync(BrowserResourceFilter filter, CancellationToken ct = default)
    {
        var entity = await ApplyFilter(_eaosDbContext.BrowserResources.AsNoTracking(), filter)
            .FirstOrDefaultAsync(ct);
        return entity is null ? null : ToBrowserRecord(entity);
    }

    public async Task<BrowserResourceRecord> SaveAsync(BrowserResourceRecord resource, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.BrowserResources.FirstOrDefaultAsync(item => item.Id == resource.Id, ct);
        if (entity is null)
        {
            entity = ToBrowserEntity(resource);
            _eaosDbContext.BrowserResources.Add(entity);
        }
        else
        {
            entity.DisplayName = BrowserResourceRecord.NormalizeName(resource.DisplayName, "Browser");
            entity.CurrentAgentId = resource.CurrentAgentId;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToBrowserRecord(entity);
    }

    public async Task<bool> DeleteAsync(BrowserResourceFilter filter, CancellationToken ct = default)
    {
        var entity = await ApplyFilter(_eaosDbContext.BrowserResources, filter).FirstOrDefaultAsync(ct);
        if (entity is null)
            return false;

        await _eaosDbContext.AgentSessionResourceAttachments
            .Where(attachment => attachment.ResourceType == AgentResourceKinds.Browser && attachment.ResourceId == entity.Id)
            .ExecuteDeleteAsync(ct);
        _eaosDbContext.BrowserResources.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetCurrentAgentAsync(Guid browserResourceId, Guid agentId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.BrowserResources.FirstOrDefaultAsync(resource => resource.Id == browserResourceId, ct);
        if (entity is null)
            return;

        entity.CurrentAgentId = agentId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static IQueryable<BrowserResourceEntity> ApplyFilter(IQueryable<BrowserResourceEntity> query, BrowserResourceFilter filter)
    {
        if (filter.Id.HasValue)
            query = query.Where(resource => resource.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(resource => resource.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(resource => resource.WorkspaceId == filter.WorkspaceId.Value);

        return query;
    }

    private static BrowserResourceRecord ToBrowserRecord(BrowserResourceEntity e) => new()
    {
        Id = e.Id,
        OwnerId = e.OwnerId,
        WorkspaceId = e.WorkspaceId,
        DisplayName = e.DisplayName,
        CurrentAgentId = e.CurrentAgentId,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static BrowserResourceEntity ToBrowserEntity(BrowserResourceRecord r) => new()
    {
        Id = r.Id,
        OwnerId = r.OwnerId,
        WorkspaceId = r.WorkspaceId,
        DisplayName = r.DisplayName,
        CurrentAgentId = r.CurrentAgentId,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
