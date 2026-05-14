namespace OffceOs.Application.Features.Integrations;

internal sealed class IntegrationDeploymentService : IIntegrationDeploymentService
{
    private readonly IIntegrationDeploymentRepository _integrationDeploymentRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IAgentLogService _agentLogService;

    public IntegrationDeploymentService(
        IIntegrationDeploymentRepository integrationDeploymentRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IAgentLogService agentLogService)
    {
        _integrationDeploymentRepository = integrationDeploymentRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _agentLogService = agentLogService;
    }

    public async Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(
        Guid actorUserId,
        Guid workspaceId,
        CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(actorUserId, workspaceId, ct);
        return await _integrationDeploymentRepository.ListAsync(
            new IntegrationDeploymentFilter { WorkspaceId = workspaceId },
            ct);
    }

    public async Task<IntegrationDeploymentRecord> DeployAsync(
        Guid actorUserId,
        Guid workspaceId,
        string integrationName,
        CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(actorUserId, workspaceId, ct);
        if (string.IsNullOrWhiteSpace(integrationName))
            throw new InvalidOperationException("Integration name is required.");

        var deployment = await _integrationDeploymentRepository.UpsertAsync(new IntegrationDeploymentRecord
        {
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
        Guid workspaceId,
        string integrationName,
        CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(actorUserId, workspaceId, ct);
        var deleted = await _integrationDeploymentRepository.DeleteAsync(
            new IntegrationDeploymentFilter
            {
                WorkspaceId = workspaceId,
                IntegrationName = integrationName.Trim(),
            },
            ct);
        if (deleted)
            await AppendDeploymentLogAsync(new IntegrationDeploymentRecord
            {
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
            MetadataJson = JsonSerializer.Serialize(new { deployment.WorkspaceId, deployment.IntegrationName, deployment.Enabled }),
        }, ct);
    }

    private async Task RequireWorkspaceEditorAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var membership = await _workspaceMemberRepository.GetByAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = userId },
            ct);
        if (membership?.Role.CanEdit() != true)
            throw new InvalidOperationException("Workspace not found.");
    }
}
