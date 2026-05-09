namespace OffceOs.Domain.Features.Management;

public sealed record UserSubscriptionFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public string? StripeCustomerId { get; init; }
    public string? StripeSubscriptionId { get; init; }
}
