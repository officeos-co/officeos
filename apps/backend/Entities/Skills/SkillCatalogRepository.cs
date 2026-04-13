namespace EnterpriseAgentOs.Api.Entities.Skills;

public interface ISkillCatalogRepository
{
    Task<IReadOnlyList<SkillRecord>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SkillRecord>> ListActiveAsync(CancellationToken ct = default);
    Task<SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<SkillRecord> UpsertAsync(SkillRecord record, CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default);
}

public sealed class SkillCatalogRepository : ISkillCatalogRepository
{
    private readonly EaosDbContext _db;

    public SkillCatalogRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SkillRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Skills.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SkillRecord>> ListActiveAsync(CancellationToken ct = default)
    {
        return await _db.Skills.AsNoTracking()
            .Where(s => s.Status == "active")
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();
        return await _db.Skills.FirstOrDefaultAsync(s => s.Name == n, ct);
    }

    public async Task<SkillRecord> UpsertAsync(SkillRecord record, CancellationToken ct = default)
    {
        var existing = await _db.Skills.FirstOrDefaultAsync(s => s.Name == record.Name, ct);
        if (existing is null)
        {
            _db.Skills.Add(record);
        }
        else
        {
            existing.Title = record.Title;
            existing.Description = record.Description;
            existing.Emoji = record.Emoji;
            existing.Doc = record.Doc;
            existing.ManifestJson = record.ManifestJson;
            existing.BundleS3Key = record.BundleS3Key;
            existing.Version = record.Version;
            existing.Status = record.Status;
            existing.BuildError = record.BuildError;
            existing.IsSystem = record.IsSystem;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return existing ?? record;
    }

    public async Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();
        var row = await _db.Skills.FirstOrDefaultAsync(s => s.Name == n, ct);
        if (row is null) return false;
        _db.Skills.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
