using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Integrations;
namespace OffceOs.Infrastructure.Features.Integrations;

internal sealed class IntegrationDeploymentRepository : IIntegrationDeploymentRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationDeploymentRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default)
    {
        var entities = await BuildQuery(filter)
            .OrderBy(d => d.IntegrationName)
            .ThenBy(d => d.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<IntegrationDeploymentRecord?> GetByAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default)
    {
        var entity = await BuildQuery(filter).FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IntegrationDeploymentRecord> UpsertAsync(IntegrationDeploymentRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.IntegrationDeployments
            .FirstOrDefaultAsync(d => d.WorkspaceId == record.WorkspaceId && d.IntegrationName == record.IntegrationName, ct);

        if (entity is null)
        {
            entity = new IntegrationDeploymentEntity
            {
                Id = record.Id,
                WorkspaceId = record.WorkspaceId,
                IntegrationName = record.IntegrationName,
                CreatedById = record.CreatedById,
                Enabled = record.Enabled,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
            };
            _eaosDbContext.IntegrationDeployments.Add(entity);
        }
        else
        {
            entity.Enabled = record.Enabled;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default)
    {
        var deleted = await BuildQuery(filter).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    private IQueryable<IntegrationDeploymentEntity> BuildQuery(IntegrationDeploymentFilter filter)
    {
        var query = _eaosDbContext.IntegrationDeployments.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(d => d.Id == filter.Id.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(d => d.WorkspaceId == filter.WorkspaceId.Value);

        if (!string.IsNullOrWhiteSpace(filter.IntegrationName))
            query = query.Where(d => d.IntegrationName == filter.IntegrationName);

        if (filter.Enabled.HasValue)
            query = query.Where(d => d.Enabled == filter.Enabled.Value);

        return query;
    }

    private static IntegrationDeploymentRecord ToRecord(IntegrationDeploymentEntity entity) => new()
    {
        Id = entity.Id,
        WorkspaceId = entity.WorkspaceId,
        IntegrationName = entity.IntegrationName,
        CreatedById = entity.CreatedById,
        Enabled = entity.Enabled,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
