namespace OffceOs.Domain.Features.Billing;

public interface IUserSubscriptionRepository
{
    Task<UserSubscriptionRecord?> GetByAsync(UserSubscriptionFilter filter, CancellationToken ct = default);
    Task AddAsync(UserSubscriptionRecord sub, CancellationToken ct = default);
    Task UpdateAsync(UserSubscriptionRecord sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
