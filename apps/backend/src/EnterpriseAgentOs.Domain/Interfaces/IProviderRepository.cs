namespace EnterpriseAgentOs.Domain.Interfaces;

public interface IProviderRepository
{
    Task<IReadOnlyList<ProviderRecord>> ListAsync(CancellationToken ct = default);
    Task<ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task SaveAsync(ProviderRecord record, CancellationToken ct = default);
    Task<bool> ClearKeyAsync(string name, CancellationToken ct = default);
}
