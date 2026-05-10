namespace OffceOs.Application.Features.Integrations;

internal sealed class IntegrationDeploymentService : IIntegrationDeploymentService
{
    private readonly IIntegrationDeploymentRepository _integrationDeploymentRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public IntegrationDeploymentService(
        IIntegrationDeploymentRepository integrationDeploymentRepository,
        IOrganizationRepository organizationRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _integrationDeploymentRepository = integrationDeploymentRepository;
        _organizationRepository = organizationRepository;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(
        Guid actorUserId,
        Guid organizationId,
        Guid? workspaceId = null,
        CancellationToken ct = default)
    {
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
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");
        if (workspace.OrganizationId != organizationId)
            throw new InvalidOperationException("Integration deployments must target workspaces in the same organization.");
        if (string.IsNullOrWhiteSpace(integrationName))
            throw new InvalidOperationException("Integration name is required.");

        return await _integrationDeploymentRepository.UpsertAsync(new IntegrationDeploymentRecord
        {
            OrganizationId = organizationId,
            WorkspaceId = workspaceId,
            IntegrationName = integrationName.Trim(),
            CreatedById = actorUserId,
            Enabled = true,
        }, ct);
    }

    public async Task<bool> RevokeAsync(
        Guid actorUserId,
        Guid organizationId,
        Guid workspaceId,
        string integrationName,
        CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        return await _integrationDeploymentRepository.DeleteAsync(
            new IntegrationDeploymentFilter
            {
                OrganizationId = organizationId,
                WorkspaceId = workspaceId,
                IntegrationName = integrationName.Trim(),
            },
            ct);
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Organization not found.");
    }
}
