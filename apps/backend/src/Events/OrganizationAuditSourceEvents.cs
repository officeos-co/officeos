namespace OffceOs.Domain.Events;

public sealed record OrganizationCreatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    string Name,
    string? ActorName) : DomainEvent;

public sealed record OrganizationRenamedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    string PreviousName,
    string Name) : DomainEvent;

public sealed record OrganizationMemberInvitedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid MemberId,
    string MemberEmail,
    string Role) : DomainEvent;

public sealed record OrganizationMemberInviteAcceptedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid MemberId,
    string MemberEmail,
    string Role) : DomainEvent;

public sealed record OrganizationMemberRemovedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid MemberId,
    Guid? MemberUserId,
    string MemberEmail,
    string Role) : DomainEvent;

public sealed record OrganizationWorkspaceCreatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    string WorkspaceName) : DomainEvent;

public sealed record WorkspaceUpdatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    string PreviousName,
    string Name) : DomainEvent;

public sealed record WorkspaceDeletedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    string WorkspaceName) : DomainEvent;

public sealed record WorkspaceMemberAddedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    Guid MemberUserId,
    string Role) : DomainEvent;

public sealed record WorkspaceMemberRoleUpdatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    Guid MemberUserId,
    string PreviousRole,
    string Role) : DomainEvent;

public sealed record WorkspaceMemberRemovedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    Guid MemberUserId) : DomainEvent;

public sealed record WorkspaceOrganizationGrantCreatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    Guid GrantedOrganizationId,
    string MaxRole) : DomainEvent;

public sealed record WorkspaceOrganizationGrantRevokedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkspaceId,
    Guid RevokedOrganizationId) : DomainEvent;

public sealed record AccessGroupCreatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid AccessGroupId,
    string Name) : DomainEvent;

public sealed record AccessGroupRenamedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid AccessGroupId,
    string PreviousName,
    string Name) : DomainEvent;

public sealed record AccessGroupDeletedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid AccessGroupId,
    string Name) : DomainEvent;

public sealed record AccessGroupMemberAddedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid AccessGroupId,
    Guid MemberUserId) : DomainEvent;

public sealed record AccessGroupMemberRemovedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid AccessGroupId,
    Guid MemberUserId) : DomainEvent;

public sealed record AccessGroupWorkspaceGrantCreatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid AccessGroupId,
    Guid WorkspaceId,
    string Role) : DomainEvent;

public sealed record AccessGroupWorkspaceGrantRevokedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid AccessGroupId,
    Guid WorkspaceId) : DomainEvent;

public sealed record OrganizationPolicyProfileUpdatedEvent(
    Guid OrganizationId,
    Guid ActorUserId,
    bool ShellToolsEnabled,
    bool FileWriteToolsEnabled,
    bool NetworkToolsEnabled,
    bool BrowserToolsEnabled,
    int AllowedToolsCount,
    int DeniedToolsCount,
    int AllowedIntegrationsCount,
    int DeniedIntegrationsCount) : DomainEvent;

public sealed record AgentToolPolicyDeniedEvent(
    Guid AgentId,
    string CorrelationId,
    string ToolName,
    string Reason) : DomainEvent;
