namespace EnterpriseAgentOs.Api.Entities.CustomSkills;

public sealed class CustomSkillRepository : ICustomSkillRepository
{
    private readonly EaosDbContext _db;

    public CustomSkillRepository(EaosDbContext db) => _db = db;

    public async Task<CustomSkillRecord> CreateAsync(CustomSkillRecord record, CancellationToken ct)
    {
        _db.CustomSkills.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<List<CustomSkillRecord>> ListAsync(CancellationToken ct)
        => await _db.CustomSkills.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public async Task<CustomSkillRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.CustomSkills.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<CustomSkillRecord?> GetByNameAsync(string name, CancellationToken ct)
        => await _db.CustomSkills.FirstOrDefaultAsync(c => c.Name == name, ct);

    public async Task UpdateAsync(CustomSkillRecord record, CancellationToken ct)
    {
        _db.CustomSkills.Update(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var record = await _db.CustomSkills.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (record is not null)
        {
            _db.CustomSkills.Remove(record);
            await _db.SaveChangesAsync(ct);
        }
    }
}
