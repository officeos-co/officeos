using EnterpriseAgentOs.Api.Entities.Providers.Models;

namespace EnterpriseAgentOs.Api.Entities.Providers.Interfaces;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default);
}
