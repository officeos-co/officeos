namespace OffceOs.Api.Features.Management;

public sealed record WorkspacePayload(
    Guid Id,
    string OwnerKind,
    Guid? OwnerUserId,
    Guid? OrganizationId,
    string Name,
    bool IsDefault,
    string? Role,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateWorkspaceInput(string Name);

public sealed record CreateOrganizationWorkspaceInput(Guid OrganizationId, string Name);

public sealed record UpdateWorkspaceInput(string? Name);

public sealed record AddWorkspaceMemberInput(Guid WorkspaceId, Guid UserId, string? Role);

public sealed record UpdateWorkspaceMemberRoleInput(Guid WorkspaceId, Guid UserId, string? Role);

public sealed record GrantWorkspaceOrganizationInput(Guid WorkspaceId, Guid OrganizationId, string? MaxRole);

public sealed record WorkspaceMemberPayload(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    string Role,
    DateTime CreatedAt);

public sealed record WorkspaceOrganizationGrantPayload(
    Guid Id,
    Guid WorkspaceId,
    Guid OrganizationId,
    string MaxRole,
    DateTime CreatedAt);

public static class WorkspaceGraphQLMapper
{
    public static WorkspacePayload ToPayload(WorkspaceRecord record) =>
        new(
            record.Id,
            record.OwnerKind.ToStorageString(),
            record.OwnerUserId,
            record.OrganizationId,
            record.Name,
            record.IsDefault,
            record.Role?.ToStorageString(),
            record.CreatedAt,
            record.UpdatedAt);

    public static WorkspaceMemberPayload ToPayload(WorkspaceMemberRecord record) =>
        new(record.Id, record.WorkspaceId, record.UserId, record.Role.ToStorageString(), record.CreatedAt);

    public static WorkspaceOrganizationGrantPayload ToPayload(WorkspaceOrganizationGrantRecord record) =>
        new(record.Id, record.WorkspaceId, record.OrganizationId, record.MaxRole.ToStorageString(), record.CreatedAt);
}
