namespace OffceOs.Domain.Features.Management;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByAsync(UserSubscriptionFilter filter, CancellationToken ct = default);
    Task AddAsync(UserSubscription sub, CancellationToken ct = default);
    Task UpdateAsync(UserSubscription sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
