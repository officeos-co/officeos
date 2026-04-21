namespace EnterpriseAgentOs.Domain.Billing;

public interface IOrgSubscriptionRepository
{
    Task<OrgSubscription?> GetByOrganizationIdAsync(string organizationId, CancellationToken ct = default);
    Task AddAsync(OrgSubscription sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
