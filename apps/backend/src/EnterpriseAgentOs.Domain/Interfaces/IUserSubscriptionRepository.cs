namespace EnterpriseAgentOs.Domain.Interfaces;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserSubscription sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
