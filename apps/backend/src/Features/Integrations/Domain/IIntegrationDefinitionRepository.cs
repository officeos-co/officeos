namespace OffceOs.Domain.Features.Integrations;

public interface IIntegrationDefinitionRepository
{
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord?> GetByNameAsync(Guid ownerId, string name, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord> UpsertAsync(Guid ownerId, IntegrationDefinitionRecord server, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerId, string name, CancellationToken ct = default);
}
