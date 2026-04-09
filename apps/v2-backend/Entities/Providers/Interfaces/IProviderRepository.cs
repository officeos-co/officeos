using EnterpriseAgentOs.Api.Entities.Providers.Models;

namespace EnterpriseAgentOs.Api.Entities.Providers.Interfaces;

public interface IProviderRepository
{
    Task<IReadOnlyList<ProviderRecord>> ListAsync(CancellationToken ct = default);
}
