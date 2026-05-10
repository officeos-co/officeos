namespace OffceOs.EventHandlers.Features.Management;

internal sealed class AuditLogHandler :
    INotificationHandler<OrganizationRenamedEvent>,
    INotificationHandler<OrganizationMemberInvitedEvent>,
    INotificationHandler<OrganizationMemberRemovedEvent>,
    INotificationHandler<OrganizationWorkspaceCreatedEvent>,
    INotificationHandler<WorkspaceUpdatedEvent>,
    INotificationHandler<WorkspaceDeletedEvent>,
    INotificationHandler<WorkspaceMemberAddedEvent>,
    INotificationHandler<WorkspaceMemberRoleUpdatedEvent>,
    INotificationHandler<WorkspaceMemberRemovedEvent>,
    INotificationHandler<WorkspaceOrganizationGrantCreatedEvent>,
    INotificationHandler<WorkspaceOrganizationGrantRevokedEvent>,
    INotificationHandler<AccessGroupCreatedEvent>,
    INotificationHandler<AccessGroupRenamedEvent>,
    INotificationHandler<AccessGroupDeletedEvent>,
    INotificationHandler<AccessGroupMemberAddedEvent>,
    INotificationHandler<AccessGroupMemberRemovedEvent>,
    INotificationHandler<AccessGroupWorkspaceGrantCreatedEvent>,
    INotificationHandler<AccessGroupWorkspaceGrantRevokedEvent>,
    INotificationHandler<OrganizationPolicyProfileUpdatedEvent>,
    INotificationHandler<OrganizationProviderProfileSavedEvent>,
    INotificationHandler<OrganizationProviderProfileDeletedEvent>,
    INotificationHandler<LlmCallCompletedEvent>,
    INotificationHandler<ToolCallCompletedEvent>,
    INotificationHandler<AgentToolPolicyDeniedEvent>
{
    private readonly IOrganizationAuditLogRepository _organizationAuditLogRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public AuditLogHandler(
        IOrganizationAuditLogRepository organizationAuditLogRepository,
        IAgentRepository agentRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _organizationAuditLogRepository = organizationAuditLogRepository;
        _agentRepository = agentRepository;
        _workspaceRepository = workspaceRepository;
    }

    public Task Handle(OrganizationRenamedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.OrganizationRenamed,
            OrganizationAuditKinds.Organization, e.OrganizationId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["previousName"] = e.PreviousName,
                ["name"] = e.Name,
            }, e.OccurredAt, ct);

    public Task Handle(OrganizationMemberInvitedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.OrganizationMemberInvited,
            OrganizationAuditKinds.OrganizationMember, e.MemberId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["memberEmail"] = e.MemberEmail,
                ["role"] = e.Role,
            }, e.OccurredAt, ct);

    public Task Handle(OrganizationMemberRemovedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.OrganizationMemberRemoved,
            OrganizationAuditKinds.OrganizationMember, e.MemberId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["memberUserId"] = e.MemberUserId,
                ["memberEmail"] = e.MemberEmail,
                ["role"] = e.Role,
            }, e.OccurredAt, ct);

    public Task Handle(OrganizationWorkspaceCreatedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceCreated,
            OrganizationAuditKinds.Workspace, e.WorkspaceId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["workspaceName"] = e.WorkspaceName }, e.OccurredAt, ct);

    public Task Handle(WorkspaceUpdatedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceUpdated,
            OrganizationAuditKinds.Workspace, e.WorkspaceId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["previousName"] = e.PreviousName,
                ["name"] = e.Name,
            }, e.OccurredAt, ct);

    public Task Handle(WorkspaceDeletedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceDeleted,
            OrganizationAuditKinds.Workspace, e.WorkspaceId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["workspaceName"] = e.WorkspaceName }, e.OccurredAt, ct);

    public Task Handle(WorkspaceMemberAddedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceMemberAdded,
            OrganizationAuditKinds.WorkspaceMember, e.MemberUserId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["role"] = e.Role }, e.OccurredAt, ct);

    public Task Handle(WorkspaceMemberRoleUpdatedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceMemberRoleUpdated,
            OrganizationAuditKinds.WorkspaceMember, e.MemberUserId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["previousRole"] = e.PreviousRole,
                ["role"] = e.Role,
            }, e.OccurredAt, ct);

    public Task Handle(WorkspaceMemberRemovedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceMemberRemoved,
            OrganizationAuditKinds.WorkspaceMember, e.MemberUserId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>(), e.OccurredAt, ct);

    public Task Handle(WorkspaceOrganizationGrantCreatedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceOrganizationGrantCreated,
            OrganizationAuditKinds.WorkspaceOrganizationGrant, e.GrantedOrganizationId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["maxRole"] = e.MaxRole }, e.OccurredAt, ct);

    public Task Handle(WorkspaceOrganizationGrantRevokedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.WorkspaceOrganizationGrantRevoked,
            OrganizationAuditKinds.WorkspaceOrganizationGrant, e.RevokedOrganizationId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>(), e.OccurredAt, ct);

    public Task Handle(AccessGroupCreatedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.AccessGroupCreated,
            OrganizationAuditKinds.AccessGroup, e.AccessGroupId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["name"] = e.Name }, e.OccurredAt, ct);

    public Task Handle(AccessGroupRenamedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.AccessGroupRenamed,
            OrganizationAuditKinds.AccessGroup, e.AccessGroupId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["previousName"] = e.PreviousName,
                ["name"] = e.Name,
            }, e.OccurredAt, ct);

    public Task Handle(AccessGroupDeletedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.AccessGroupDeleted,
            OrganizationAuditKinds.AccessGroup, e.AccessGroupId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["name"] = e.Name }, e.OccurredAt, ct);

    public Task Handle(AccessGroupMemberAddedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.AccessGroupMemberAdded,
            OrganizationAuditKinds.AccessGroupMember, e.MemberUserId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["accessGroupId"] = e.AccessGroupId }, e.OccurredAt, ct);

    public Task Handle(AccessGroupMemberRemovedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.AccessGroupMemberRemoved,
            OrganizationAuditKinds.AccessGroupMember, e.MemberUserId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["accessGroupId"] = e.AccessGroupId }, e.OccurredAt, ct);

    public Task Handle(AccessGroupWorkspaceGrantCreatedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.AccessGroupWorkspaceGrantCreated,
            OrganizationAuditKinds.AccessGroupWorkspaceGrant, e.WorkspaceId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["accessGroupId"] = e.AccessGroupId,
                ["role"] = e.Role,
            }, e.OccurredAt, ct);

    public Task Handle(AccessGroupWorkspaceGrantRevokedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, e.WorkspaceId, null, OrganizationAuditKinds.AccessGroupWorkspaceGrantRevoked,
            OrganizationAuditKinds.AccessGroupWorkspaceGrant, e.WorkspaceId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["accessGroupId"] = e.AccessGroupId }, e.OccurredAt, ct);

    public Task Handle(OrganizationPolicyProfileUpdatedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.OrganizationPolicyUpdated,
            OrganizationAuditKinds.OrganizationPolicy, e.OrganizationId, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["shellToolsEnabled"] = e.ShellToolsEnabled,
                ["fileWriteToolsEnabled"] = e.FileWriteToolsEnabled,
                ["networkToolsEnabled"] = e.NetworkToolsEnabled,
                ["browserToolsEnabled"] = e.BrowserToolsEnabled,
                ["allowedToolsCount"] = e.AllowedToolsCount,
                ["deniedToolsCount"] = e.DeniedToolsCount,
                ["allowedIntegrationsCount"] = e.AllowedIntegrationsCount,
                ["deniedIntegrationsCount"] = e.DeniedIntegrationsCount,
            }, e.OccurredAt, ct);

    public Task Handle(OrganizationProviderProfileSavedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.ProviderProfileSaved,
            OrganizationAuditKinds.ProviderProfile, e.Provider, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?>
            {
                ["provider"] = e.Provider,
                ["displayName"] = e.DisplayName,
                ["authKind"] = e.AuthKind,
                ["allowedModelsCount"] = e.AllowedModelsCount,
                ["enabled"] = e.Enabled,
            }, e.OccurredAt, ct);

    public Task Handle(OrganizationProviderProfileDeletedEvent e, CancellationToken ct)
        => SaveAsync(e.OrganizationId, e.ActorUserId, null, null, OrganizationAuditKinds.ProviderProfileDeleted,
            OrganizationAuditKinds.ProviderProfile, e.Provider, OrganizationAuditKinds.Success, null,
            new Dictionary<string, object?> { ["provider"] = e.Provider }, e.OccurredAt, ct);

    public async Task Handle(LlmCallCompletedEvent e, CancellationToken ct)
    {
        var scope = await GetAgentScopeAsync(e.AgentId, ct);
        if (scope is null)
            return;
        var value = scope.Value;

        await SaveAsync(value.OrganizationId, value.ActorUserId, value.WorkspaceId, e.AgentId,
            OrganizationAuditKinds.AgentProviderModelUsed, OrganizationAuditKinds.Agent, e.AgentId,
            OrganizationAuditKinds.Success, e.CorrelationId,
            new Dictionary<string, object?>
            {
                ["provider"] = e.Provider,
                ["model"] = e.Model,
                ["inputTokens"] = e.InputTokens,
                ["outputTokens"] = e.OutputTokens,
                ["durationMs"] = e.DurationMs,
            }, e.OccurredAt, ct);
    }

    public async Task Handle(ToolCallCompletedEvent e, CancellationToken ct)
    {
        var scope = await GetAgentScopeAsync(e.AgentId, ct);
        if (scope is null)
            return;
        var value = scope.Value;

        await SaveAsync(value.OrganizationId, value.ActorUserId, value.WorkspaceId, e.AgentId,
            OrganizationAuditKinds.AgentToolUsed, OrganizationAuditKinds.Tool, e.ToolName,
            e.Success ? OrganizationAuditKinds.Success : OrganizationAuditKinds.Failure, e.CorrelationId,
            new Dictionary<string, object?>
            {
                ["toolName"] = e.ToolName,
                ["durationMs"] = e.DurationMs,
                ["outputLength"] = e.Output.Length,
            }, e.OccurredAt, ct);
    }

    public async Task Handle(AgentToolPolicyDeniedEvent e, CancellationToken ct)
    {
        var scope = await GetAgentScopeAsync(e.AgentId, ct);
        if (scope is null)
            return;
        var value = scope.Value;

        await SaveAsync(value.OrganizationId, value.ActorUserId, value.WorkspaceId, e.AgentId,
            OrganizationAuditKinds.AgentToolPolicyDenied, OrganizationAuditKinds.Tool, e.ToolName,
            OrganizationAuditKinds.Denied, e.CorrelationId,
            new Dictionary<string, object?>
            {
                ["toolName"] = e.ToolName,
                ["reason"] = e.Reason,
            }, e.OccurredAt, ct);
    }

    private async Task<(Guid OrganizationId, Guid? ActorUserId, Guid WorkspaceId)?> GetAgentScopeAsync(Guid agentId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
        if (agent?.WorkspaceId is null)
            return null;

        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = agent.WorkspaceId.Value }, ct);
        if (workspace?.OrganizationId is null)
            return null;

        return (workspace.OrganizationId.Value, agent.OwnerId, workspace.Id);
    }

    private Task SaveAsync(
        Guid organizationId,
        Guid? actorUserId,
        Guid? workspaceId,
        Guid? agentId,
        string action,
        string resourceType,
        Guid resourceId,
        string outcome,
        string? correlationId,
        IReadOnlyDictionary<string, object?> metadata,
        DateTime occurredAt,
        CancellationToken ct)
        => SaveAsync(organizationId, actorUserId, workspaceId, agentId, action, resourceType,
            resourceId.ToString("N"), outcome, correlationId, metadata, occurredAt, ct);

    private Task SaveAsync(
        Guid organizationId,
        Guid? actorUserId,
        Guid? workspaceId,
        Guid? agentId,
        string action,
        string resourceType,
        string resourceId,
        string outcome,
        string? correlationId,
        IReadOnlyDictionary<string, object?> metadata,
        DateTime occurredAt,
        CancellationToken ct)
        => _organizationAuditLogRepository.SaveAsync(new OrganizationAuditLogRecord
        {
            OrganizationId = organizationId,
            ActorUserId = actorUserId,
            WorkspaceId = workspaceId,
            AgentId = agentId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Outcome = outcome,
            CorrelationId = correlationId,
            MetadataJson = OrganizationAuditMetadataPolicy.FromMetadata(metadata),
            OccurredAt = occurredAt,
        }, ct);
}
