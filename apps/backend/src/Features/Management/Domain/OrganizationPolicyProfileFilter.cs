namespace OffceOs.Domain.Features.Management;

public sealed record OrganizationPolicyProfileFilter
{
    public Guid? Id { get; init; }
    public Guid? OrganizationId { get; init; }
}
