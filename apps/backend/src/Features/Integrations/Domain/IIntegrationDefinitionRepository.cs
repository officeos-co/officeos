namespace OffceOs.Features.Integrations.Domain;

public interface IIntegrationDefinitionRepository
{
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord?> GetByNameAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default);
    Task<IntegrationDefinitionRecord> UpsertAsync(Guid ownerId, Guid workspaceId, IntegrationDefinitionRecord server, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default);
}
