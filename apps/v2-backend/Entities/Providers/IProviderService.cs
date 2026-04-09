using EnterpriseAgentOs.Api.Entities.Providers;

namespace EnterpriseAgentOs.Api.Entities.Providers;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default);
}
