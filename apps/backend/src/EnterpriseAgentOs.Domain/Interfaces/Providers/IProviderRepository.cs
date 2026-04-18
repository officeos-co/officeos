namespace EnterpriseAgentOs.Domain.Interfaces.Providers;

public interface IProviderRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.ProviderRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task SaveAsync(EnterpriseAgentOs.Domain.Models.ProviderRecord record, CancellationToken ct = default);
    Task<bool> ClearKeyAsync(string name, CancellationToken ct = default);
}
