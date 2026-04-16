namespace EnterpriseAgentOs.Api.Entities.SkillRegistry;

public sealed class SkillRegistryRepository : ISkillRegistryRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public SkillRegistryRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db) => _db = db;

    public async Task<List<EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord>> ListAsync(CancellationToken ct)
        => await _db.SkillRegistry.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public async Task<EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord?> GetByNameAsync(string name, CancellationToken ct)
        => await _db.SkillRegistry.FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord> CreateAsync(EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord record, CancellationToken ct)
    {
        _db.SkillRegistry.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task UpdateAsync(EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord record, CancellationToken ct)
    {
        record.UpdatedAt = DateTime.UtcNow;
        _db.SkillRegistry.Update(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteByNameAsync(string name, CancellationToken ct)
    {
        var record = await _db.SkillRegistry.FirstOrDefaultAsync(r => r.Name == name, ct);
        if (record is not null)
        {
            _db.SkillRegistry.Remove(record);
            await _db.SaveChangesAsync(ct);
        }
    }
}
