namespace OffceOs.Domain.Features.Management;

public interface IOrganizationService
{
    Task<OrganizationOverview> GetOverviewAsync(Guid callerUserId, string callerEmail, string? callerName, CancellationToken ct = default);
    Task<OrgMemberRecord> InviteMemberAsync(Guid callerUserId, string callerEmail, string? callerName, string memberEmail, string? role, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid callerUserId, string callerEmail, string? callerName, Guid memberId, CancellationToken ct = default);
    Task<OrganizationRecord> RenameAsync(Guid callerUserId, string callerEmail, string? callerName, string name, CancellationToken ct = default);
    Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(Guid callerUserId, string callerEmail, string? callerName, CancellationToken ct = default);
}

public sealed record OrganizationOverview(
    OrganizationRecord Organization,
    IReadOnlyList<OrgMemberRecord> Members);
