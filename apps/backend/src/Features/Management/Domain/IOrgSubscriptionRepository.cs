namespace EnterpriseAgentOs.Domain.Features.Management;

public interface IOrgSubscriptionRepository
{
    Task<OrgSubscription?> GetByAsync(OrgSubscriptionFilter filter, CancellationToken ct = default);
    Task AddAsync(OrgSubscription sub, CancellationToken ct = default);
    Task UpdateAsync(OrgSubscription sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
