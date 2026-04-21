namespace EnterpriseAgentOs.Infrastructure.Persistence;

internal sealed class SkillRepository : ISkillRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public SkillRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<SkillCredentialRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _eaosDbContext.SkillCredentials.AsNoTracking().OrderBy(s => s.SkillName).ToListAsync(ct);
    }

    public async Task<SkillCredentialRecord?> GetByNameAsync(string skillName, CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        return await _eaosDbContext.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
    }

    public async Task<SkillCredentialRecord> UpsertAsync(
        string skillName,
        bool? enabled,
        string? encryptedCredentials,
        CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var row = await _eaosDbContext.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        if (row is null)
        {
            row = new SkillCredentialRecord
            {
                SkillName = name,
                Enabled = enabled ?? false,
                EncryptedCredentials = encryptedCredentials,
                ConfiguredAt = encryptedCredentials is null ? null : DateTime.UtcNow,
            };
            _eaosDbContext.SkillCredentials.Add(row);
        }
        else
        {
            if (enabled.HasValue) row.Enabled = enabled.Value;
            if (encryptedCredentials is not null)
            {
                row.EncryptedCredentials = encryptedCredentials;
                row.ConfiguredAt = DateTime.UtcNow;
            }
        }
        await _eaosDbContext.SaveChangesAsync(ct);
        return row;
    }

    public async Task<bool> DeleteByNameAsync(string skillName, CancellationToken ct = default)
    {
        var row = await GetByNameAsync(skillName, ct);
        if (row is null) return false;
        _eaosDbContext.SkillCredentials.Remove(row);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetRunTargetAsync(string skillName, string? runTarget, CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var row = await _eaosDbContext.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        if (row is null)
        {
            row = new SkillCredentialRecord { SkillName = name, RunTarget = runTarget };
            _eaosDbContext.SkillCredentials.Add(row);
        }
        else
        {
            row.RunTarget = runTarget;
        }
        await _eaosDbContext.SaveChangesAsync(ct);
    }
}
