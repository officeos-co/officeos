namespace EnterpriseAgentOs.Infrastructure.Features.Providers;

internal sealed class ProviderRepository : IProviderRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public ProviderRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<ProviderRecord>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.Providers.AsNoTracking().OrderBy(p => p.DisplayName).ToListAsync(ct);
        return entities.Select(ToProviderRecord).ToList();
    }

    public async Task<ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
        return entity is null ? null : ToProviderRecord(entity);
    }

    public async Task SaveAsync(ProviderRecord record, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.Providers.FirstOrDefaultAsync(p => p.Name == record.Name, ct);
        if (existing is null)
        {
            _eaosDbContext.Providers.Add(ToProviderEntity(record));
        }
        else
        {
            existing.Name = record.Name;
            existing.DisplayName = record.DisplayName;
            existing.EncryptedApiKey = record.EncryptedApiKey;
            existing.ConfiguredAt = record.ConfiguredAt;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> ClearKeyAsync(string name, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
        if (entity is null)
        {
            return false;
        }

        entity.EncryptedApiKey = null;
        entity.ConfiguredAt = null;
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static ProviderRecord ToProviderRecord(ProviderEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        DisplayName = e.DisplayName,
        EncryptedApiKey = e.EncryptedApiKey,
        ConfiguredAt = e.ConfiguredAt,
    };

    private static ProviderEntity ToProviderEntity(ProviderRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        DisplayName = r.DisplayName,
        EncryptedApiKey = r.EncryptedApiKey,
        ConfiguredAt = r.ConfiguredAt,
    };
}
