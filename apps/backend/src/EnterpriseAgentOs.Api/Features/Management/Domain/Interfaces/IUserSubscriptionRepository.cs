namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record UserSubscriptionFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public string? StripeCustomerId { get; init; }
    public string? StripeSubscriptionId { get; init; }
}

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByAsync(UserSubscriptionFilter filter, CancellationToken ct = default);
    Task AddAsync(UserSubscription sub, CancellationToken ct = default);
    Task UpdateAsync(UserSubscription sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
