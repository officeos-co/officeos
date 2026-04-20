namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public ProviderRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<ProviderRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _eaosDbContext.Providers.AsNoTracking().OrderBy(p => p.DisplayName).ToListAsync(ct);
    }

    public async Task<ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _eaosDbContext.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
    }

    public async Task SaveAsync(ProviderRecord record, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.Providers.FirstOrDefaultAsync(p => p.Name == record.Name, ct);
        if (existing is null)
            _eaosDbContext.Providers.Add(record);
        else
            _eaosDbContext.Entry(existing).CurrentValues.SetValues(record);

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> ClearKeyAsync(string name, CancellationToken ct = default)
    {
        var record = await _eaosDbContext.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
        if (record is null)
        {
            return false;
        }

        record.EncryptedApiKey = null;
        record.ConfiguredAt = null;
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }
}
