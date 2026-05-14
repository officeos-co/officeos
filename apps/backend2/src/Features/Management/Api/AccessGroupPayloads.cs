namespace OffceOs.Api.Features.Management;

public sealed record AccessGroupPayload(Guid Id, Guid OrganizationId, string Name, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record AccessGroupMemberPayload(Guid Id, Guid AccessGroupId, Guid UserId, DateTime CreatedAt);

public sealed record AccessGroupWorkspaceGrantPayload(Guid Id, Guid AccessGroupId, Guid WorkspaceId, string Role, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record CreateAccessGroupInput(Guid OrganizationId, string Name);

public sealed record RenameAccessGroupInput(Guid AccessGroupId, string Name);

public sealed record AddAccessGroupMemberInput(Guid AccessGroupId, Guid UserId);

public sealed record GrantAccessGroupWorkspaceInput(Guid AccessGroupId, Guid WorkspaceId, string? Role);

internal static class AccessGroupGraphQLMapper
{
    public static AccessGroupPayload ToPayload(AccessGroupRecord record) =>
        new(record.Id, record.OrganizationId, record.Name, record.CreatedAt, record.UpdatedAt);

    public static AccessGroupMemberPayload ToPayload(AccessGroupMemberRecord record) =>
        new(record.Id, record.AccessGroupId, record.UserId, record.CreatedAt);

    public static AccessGroupWorkspaceGrantPayload ToPayload(AccessGroupWorkspaceGrantRecord record) =>
        new(record.Id, record.AccessGroupId, record.WorkspaceId, record.Role.ToStorageString(), record.CreatedAt, record.UpdatedAt);
}
