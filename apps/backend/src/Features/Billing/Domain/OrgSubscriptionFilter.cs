namespace OffceOs.Domain.Features.Billing;

public sealed record OrgSubscriptionFilter
{
    public Guid? Id { get; init; }
    public string? OrganizationId { get; init; }
    public string? StripeCustomerId { get; init; }
    public string? StripeSubscriptionId { get; init; }
}
