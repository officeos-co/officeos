namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record OrgMemberFilter
{
    public Guid? Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
}
