
namespace EnterpriseAgentOs.Api.Entities.Providers;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly EaosDbContext _db;

    public ProviderRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProviderRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Providers.AsNoTracking().OrderBy(p => p.DisplayName).ToListAsync(ct);
    }

    public async Task<ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _db.Providers.FirstOrDefaultAsync(p => p.Name == name, ct);
    }

    public async Task SaveAsync(ProviderRecord record, CancellationToken ct = default)
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
