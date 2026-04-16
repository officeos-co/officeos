namespace EnterpriseAgentOs.Api.Entities.Skills;

public interface ISkillCatalogRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.SkillRecord>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.SkillRecord>> ListActiveAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.SkillRecord> UpsertAsync(EnterpriseAgentOs.Api.Database.Models.SkillRecord record, CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default);
}

public sealed class SkillCatalogRepository : ISkillCatalogRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public SkillCatalogRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.SkillRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Skills.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.SkillRecord>> ListActiveAsync(CancellationToken ct = default)
    {
        return await _db.Skills.AsNoTracking()
            .Where(s => s.Status == "active")
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();
        return await _db.Skills.FirstOrDefaultAsync(s => s.Name == n, ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.SkillRecord> UpsertAsync(EnterpriseAgentOs.Api.Database.Models.SkillRecord record, CancellationToken ct = default)
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
