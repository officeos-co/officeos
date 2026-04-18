namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public ProviderRepository(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.ProviderRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Providers.AsNoTracking().OrderBy(p => p.DisplayName).ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _db.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
    }

    public async Task SaveAsync(EnterpriseAgentOs.Domain.Models.ProviderRecord record, CancellationToken ct = default)
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
