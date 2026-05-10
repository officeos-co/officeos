namespace OffceOs.Domain.Features.Billing;

public interface IOrgSubscriptionRepository
{
    Task<OrgSubscriptionRecord?> GetByAsync(OrgSubscriptionFilter filter, CancellationToken ct = default);
    Task AddAsync(OrgSubscriptionRecord sub, CancellationToken ct = default);
    Task UpdateAsync(OrgSubscriptionRecord sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
