namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class OrgSubscriptionRepository : IOrgSubscriptionRepository
{
    private readonly EaosDbContext _db;

    public OrgSubscriptionRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<OrgSubscription?> GetByOrganizationIdAsync(string organizationId, CancellationToken ct = default)
    {
        return await _db.OrgSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
    }

    public async Task AddAsync(OrgSubscription sub, CancellationToken ct = default)
    {
        await _db.OrgSubscriptions.AddAsync(sub, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
