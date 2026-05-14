namespace OffceOs.Domain.Features.Integrations;

public interface IIntegrationDeploymentService
{
    Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(Guid actorUserId, Guid organizationId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<IntegrationDeploymentRecord> DeployAsync(Guid actorUserId, Guid organizationId, Guid workspaceId, string integrationName, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid actorUserId, Guid organizationId, Guid workspaceId, string integrationName, CancellationToken ct = default);
}
