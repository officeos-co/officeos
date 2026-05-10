namespace OffceOs.Domain.Features.Management;

public sealed record AccessGroupFilter
{
    public Guid? Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? WorkspaceId { get; init; }
}
