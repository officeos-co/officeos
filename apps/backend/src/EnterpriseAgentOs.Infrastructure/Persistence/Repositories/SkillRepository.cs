namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class SkillRepository : ISkillRepository
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public SkillRepository(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.SkillCredentials.AsNoTracking().OrderBy(s => s.SkillName).ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord?> GetByNameAsync(string skillName, CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        return await _db.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord> UpsertAsync(
        string skillName,
        bool? enabled,
        string? encryptedCredentials,
        CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var row = await _db.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        if (row is null)
        {
            row = new EnterpriseAgentOs.Domain.Models.SkillCredentialRecord
            {
                SkillName = name,
                Enabled = enabled ?? false,
                EncryptedCredentials = encryptedCredentials,
                ConfiguredAt = encryptedCredentials is null ? null : DateTime.UtcNow,
            };
            _db.SkillCredentials.Add(row);
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
        await _db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<bool> DeleteByNameAsync(string skillName, CancellationToken ct = default)
    {
        var row = await GetByNameAsync(skillName, ct);
        if (row is null) return false;
        _db.SkillCredentials.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetRunTargetAsync(string skillName, string? runTarget, CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var row = await _db.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        if (row is null)
        {
            row = new EnterpriseAgentOs.Domain.Models.SkillCredentialRecord { SkillName = name, RunTarget = runTarget };
            _db.SkillCredentials.Add(row);
        }
        else
        {
            row.RunTarget = runTarget;
        }
        await _db.SaveChangesAsync(ct);
    }
}
