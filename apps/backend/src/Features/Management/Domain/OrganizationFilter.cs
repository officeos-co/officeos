namespace OffceOs.Domain.Features.Management;

public sealed record OrganizationFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerUserId { get; init; }
    public string? Name { get; init; }
}
