namespace EnterpriseAgentOs.Api.Entities.Providers;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public ProviderRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.ProviderRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Providers.AsNoTracking().OrderBy(p => p.DisplayName).ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _db.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
    }

    public async Task SaveAsync(EnterpriseAgentOs.Api.Database.Models.ProviderRecord record, CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ClearKeyAsync(string name, CancellationToken ct = default)
    {
        var record = await _db.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
        if (record is null)
        {
            return false;
        }

        record.EncryptedApiKey = null;
        record.ConfiguredAt = null;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
