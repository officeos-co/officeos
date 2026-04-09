namespace EnterpriseAgentOs.Api.Entities.Providers;

public interface IProviderRepository
{
    Task<IReadOnlyList<ProviderRecord>> ListAsync(CancellationToken ct = default);
    Task<ProviderRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(ProviderRecord record, CancellationToken ct = default);
    Task<bool> ClearKeyAsync(Guid id, CancellationToken ct = default);
}
