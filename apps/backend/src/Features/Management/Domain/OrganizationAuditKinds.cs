namespace OffceOs.Domain.Features.Management;

public static class OrganizationAuditKinds
{
    public const string Success = "success";
    public const string Failure = "failure";
    public const string Denied = "denied";

    public const string Organization = "organization";
    public const string OrganizationMember = "organization_member";
    public const string Workspace = "workspace";
    public const string WorkspaceMember = "workspace_member";
    public const string WorkspaceOrganizationGrant = "workspace_organization_grant";
    public const string AccessGroup = "access_group";
    public const string AccessGroupMember = "access_group_member";
    public const string AccessGroupWorkspaceGrant = "access_group_workspace_grant";
    public const string OrganizationPolicy = "organization_policy";
    public const string ProviderProfile = "provider_profile";
    public const string Agent = "agent";
    public const string Tool = "tool";

    public const string OrganizationRenamed = "organization.renamed";
    public const string OrganizationMemberInvited = "organization.member.invited";
    public const string OrganizationMemberRemoved = "organization.member.removed";
    public const string WorkspaceCreated = "workspace.created";
    public const string WorkspaceUpdated = "workspace.updated";
    public const string WorkspaceDeleted = "workspace.deleted";
    public const string WorkspaceMemberAdded = "workspace.member.added";
    public const string WorkspaceMemberRoleUpdated = "workspace.member.role_updated";
    public const string WorkspaceMemberRemoved = "workspace.member.removed";
    public const string WorkspaceOrganizationGrantCreated = "workspace.organization_grant.created";
    public const string WorkspaceOrganizationGrantRevoked = "workspace.organization_grant.revoked";
    public const string AccessGroupCreated = "access_group.created";
    public const string AccessGroupRenamed = "access_group.renamed";
    public const string AccessGroupDeleted = "access_group.deleted";
    public const string AccessGroupMemberAdded = "access_group.member.added";
    public const string AccessGroupMemberRemoved = "access_group.member.removed";
    public const string AccessGroupWorkspaceGrantCreated = "access_group.workspace_grant.created";
    public const string AccessGroupWorkspaceGrantRevoked = "access_group.workspace_grant.revoked";
    public const string OrganizationPolicyUpdated = "organization.policy.updated";
    public const string ProviderProfileSaved = "provider.profile.saved";
    public const string ProviderProfileDeleted = "provider.profile.deleted";
    public const string AgentProviderModelUsed = "agent.provider_model.used";
    public const string AgentToolUsed = "agent.tool.used";
    public const string AgentToolPolicyDenied = "agent.tool.policy_denied";
}
