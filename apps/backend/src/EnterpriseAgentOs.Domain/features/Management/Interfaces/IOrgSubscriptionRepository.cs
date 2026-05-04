namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record OrgSubscriptionFilter
{
    public Guid? Id { get; init; }
    public string? OrganizationId { get; init; }
    public string? StripeCustomerId { get; init; }
    public string? StripeSubscriptionId { get; init; }
}

public interface IOrgSubscriptionRepository
{
    Task<OrgSubscription?> GetByAsync(OrgSubscriptionFilter filter, CancellationToken ct = default);
    Task AddAsync(OrgSubscription sub, CancellationToken ct = default);
    Task UpdateAsync(OrgSubscription sub, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
