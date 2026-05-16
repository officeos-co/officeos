namespace OffceOs.Features.Integrations.Domain;

public interface IIntegrationDeploymentService
{
    Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(Guid actorUserId, Guid workspaceId, CancellationToken ct = default);
    Task<IntegrationDeploymentRecord> DeployAsync(Guid actorUserId, Guid workspaceId, string integrationName, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid actorUserId, Guid workspaceId, string integrationName, CancellationToken ct = default);
}
