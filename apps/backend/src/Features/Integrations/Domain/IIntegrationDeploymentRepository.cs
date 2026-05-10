namespace OffceOs.Domain.Features.Integrations;

public interface IIntegrationDeploymentRepository
{
    Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default);
    Task<IntegrationDeploymentRecord?> GetByAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default);
    Task<IntegrationDeploymentRecord> UpsertAsync(IntegrationDeploymentRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default);
}
