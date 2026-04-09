using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Entities.Providers;
using Microsoft.EntityFrameworkCore;

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
}
