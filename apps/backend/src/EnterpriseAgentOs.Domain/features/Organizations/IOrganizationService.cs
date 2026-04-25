namespace EnterpriseAgentOs.Domain.Features.Organizations;

public interface IOrganizationService
{
    Task<OrgMemberRecord> InviteMemberAsync(Guid callerUserId, string callerEmail, string? callerName, string memberEmail, string? role, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid callerUserId, string callerEmail, string? callerName, Guid memberId, CancellationToken ct = default);
    Task<OrganizationRecord> RenameAsync(Guid callerUserId, string callerEmail, string? callerName, string name, CancellationToken ct = default);
    Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(Guid callerUserId, string callerEmail, string? callerName, CancellationToken ct = default);
}
