namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record OrganizationFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerUserId { get; init; }
    public string? Name { get; init; }
}

public sealed record OrgMemberFilter
{
    public Guid? Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
}

public interface IOrganizationRepository
{
    Task<OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default);

    Task<OrganizationRecord?> GetByAsync(OrganizationFilter filter, CancellationToken ct = default);

    Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<OrgMemberRecord> AddMemberAsync(OrgMemberRecord member, CancellationToken ct = default);

    Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default);

    Task<OrganizationRecord> RenameAsync(
        Guid organizationId,
        string name,
        CancellationToken ct = default);
}
