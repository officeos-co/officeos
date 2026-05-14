namespace OffceOs.Application.Features.Integrations;

internal sealed class IntegrationDeploymentService : IIntegrationDeploymentService
{
    private readonly IIntegrationDeploymentRepository _integrationDeploymentRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IAgentLogService _agentLogService;

    public IntegrationDeploymentService(
        IIntegrationDeploymentRepository integrationDeploymentRepository,
        IOrganizationRepository organizationRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IAgentLogService agentLogService)
    {
        _integrationDeploymentRepository = integrationDeploymentRepository;
        _organizationRepository = organizationRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _agentLogService = agentLogService;
    }

    public async Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(
        Guid actorUserId,
        Guid organizationId,
        Guid? workspaceId = null,
        CancellationToken ct = default)
    {
        if (workspaceId.HasValue)
            await RequireWorkspaceEditorAsync(actorUserId, organizationId, workspaceId.Value, ct);
        else
            await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        return await _integrationDeploymentRepository.ListAsync(
            new IntegrationDeploymentFilter { OrganizationId = organizationId, WorkspaceId = workspaceId },
            ct);
    }

    public async Task<IntegrationDeploymentRecord> DeployAsync(
        Guid actorUserId,
        Guid organizationId,
        Guid workspaceId,
        string integrationName,
        CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(actorUserId, organizationId, workspaceId, ct);
        if (string.IsNullOrWhiteSpace(integrationName))
            throw new InvalidOperationException("Integration name is required.");

        var deployment = await _integrationDeploymentRepository.UpsertAsync(new IntegrationDeploymentRecord
        {
            OrganizationId = organizationId,
            WorkspaceId = workspaceId,
            IntegrationName = integrationName.Trim(),
            CreatedById = actorUserId,
            Enabled = true,
        }, ct);
        await AppendDeploymentLogAsync(deployment, AgentLogType.System, $"Integration '{deployment.IntegrationName}' deployed.", ct);
        return deployment;
    }

    public async Task<bool> RevokeAsync(
        Guid actorUserId,
        Guid organizationId,
        Guid workspaceId,
        string integrationName,
        CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(actorUserId, organizationId, workspaceId, ct);
        var deleted = await _integrationDeploymentRepository.DeleteAsync(
            new IntegrationDeploymentFilter
            {
                OrganizationId = organizationId,
                WorkspaceId = workspaceId,
                IntegrationName = integrationName.Trim(),
            },
            ct);
        if (deleted)
            await AppendDeploymentLogAsync(new IntegrationDeploymentRecord
            {
                OrganizationId = organizationId,
                WorkspaceId = workspaceId,
                IntegrationName = integrationName.Trim(),
                CreatedById = actorUserId,
            }, AgentLogType.System, $"Integration '{integrationName.Trim()}' revoked.", ct);
        return deleted;
    }

    private Task AppendDeploymentLogAsync(IntegrationDeploymentRecord deployment, AgentLogType type, string content, CancellationToken ct)
    {
        return _agentLogService.AppendAsync(new AgentLogRecord
        {
            WorkspaceId = deployment.WorkspaceId,
            ResourceKind = ResourceLogKinds.IntegrationDeployment,
            ResourceId = deployment.Id,
            ResourceName = deployment.IntegrationName.Trim().ToLowerInvariant(),
            Type = type,
            Integration = deployment.IntegrationName.Trim().ToLowerInvariant(),
            Content = content,
            MetadataJson = JsonSerializer.Serialize(new { deployment.OrganizationId, deployment.IntegrationName, deployment.Enabled }),
        }, ct);
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Organization not found.");
    }

    private async Task RequireWorkspaceEditorAsync(Guid userId, Guid organizationId, Guid workspaceId, CancellationToken ct)
    {
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");
        if (workspace.OrganizationId != organizationId)
            throw new InvalidOperationException("Integration deployments must target workspaces in the same organization.");

        var membership = await _workspaceMemberRepository.GetByAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = userId },
            ct);
        if (membership?.Role.CanEdit() == true)
            return;

        await RequireOrganizationAdminAsync(userId, organizationId, ct);
    }
}
