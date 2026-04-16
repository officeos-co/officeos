namespace EnterpriseAgentOs.Api.Entities.Providers;

public interface IProviderRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.ProviderRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.ProviderRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task SaveAsync(EnterpriseAgentOs.Api.Database.Models.ProviderRecord record, CancellationToken ct = default);
    Task<bool> ClearKeyAsync(string name, CancellationToken ct = default);
}
