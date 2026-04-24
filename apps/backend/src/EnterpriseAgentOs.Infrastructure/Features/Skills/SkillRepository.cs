namespace EnterpriseAgentOs.Infrastructure.Features.Skills;

internal sealed class SkillRepository : ISkillRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public SkillRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<SkillCredentialRecord>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.SkillCredentials.AsNoTracking().OrderBy(s => s.SkillName).ToListAsync(ct);
        return entities.Select(ToSkillCredentialRecord).ToList();
    }

    public async Task<SkillCredentialRecord?> GetByNameAsync(string skillName, CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var entity = await _eaosDbContext.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        return entity is null ? null : ToSkillCredentialRecord(entity);
    }

    public async Task<SkillCredentialRecord> UpsertAsync(
        string skillName,
        bool? enabled,
        string? encryptedCredentials,
        CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var entity = await _eaosDbContext.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        if (entity is null)
        {
            entity = new SkillCredentialEntity
            {
                Id = Guid.NewGuid(),
                SkillName = name,
                Enabled = enabled ?? false,
                EncryptedCredentials = encryptedCredentials,
                ConfiguredAt = encryptedCredentials is null ? null : DateTime.UtcNow,
            };
            _eaosDbContext.SkillCredentials.Add(entity);
        }
        else
        {
            if (enabled.HasValue) entity.Enabled = enabled.Value;
            if (encryptedCredentials is not null)
            {
                entity.EncryptedCredentials = encryptedCredentials;
                entity.ConfiguredAt = DateTime.UtcNow;
            }
        }
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToSkillCredentialRecord(entity);
    }

    public async Task<bool> DeleteByNameAsync(string skillName, CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var entity = await _eaosDbContext.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        if (entity is null) return false;
        _eaosDbContext.SkillCredentials.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetRunTargetAsync(string skillName, string? runTarget, CancellationToken ct = default)
    {
        var name = skillName.Trim().ToLowerInvariant();
        var entity = await _eaosDbContext.SkillCredentials.FirstOrDefaultAsync(s => s.SkillName == name, ct);
        if (entity is null)
        {
            entity = new SkillCredentialEntity { Id = Guid.NewGuid(), SkillName = name, RunTarget = runTarget };
            _eaosDbContext.SkillCredentials.Add(entity);
        }
        else
        {
            entity.RunTarget = runTarget;
        }
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static SkillCredentialRecord ToSkillCredentialRecord(SkillCredentialEntity e) => new()
    {
        Id = e.Id,
        SkillName = e.SkillName,
        Enabled = e.Enabled,
        EncryptedCredentials = e.EncryptedCredentials,
        ConfiguredAt = e.ConfiguredAt,
        RunTarget = e.RunTarget,
    };

    private static SkillCredentialEntity ToSkillCredentialEntity(SkillCredentialRecord r) => new()
    {
        Id = r.Id,
        SkillName = r.SkillName,
        Enabled = r.Enabled,
        EncryptedCredentials = r.EncryptedCredentials,
        ConfiguredAt = r.ConfiguredAt,
        RunTarget = r.RunTarget,
    };
}
