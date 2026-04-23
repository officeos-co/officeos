namespace EnterpriseAgentOs.Infrastructure.Features.Billing;

internal sealed class OrgSubscriptionRepository : IOrgSubscriptionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrgSubscriptionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<OrgSubscription?> GetByOrganizationIdAsync(string organizationId, CancellationToken ct = default)
    {
        return await _eaosDbContext.OrgSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
    }

    public async Task AddAsync(OrgSubscription sub, CancellationToken ct = default)
    {
        await _eaosDbContext.OrgSubscriptions.AddAsync(sub, ct);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _eaosDbContext.SaveChangesAsync(ct);
    }
}
