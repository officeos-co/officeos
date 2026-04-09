using EnterpriseAgentOs.Api.Entities.Providers;

namespace EnterpriseAgentOs.Api.Entities.Providers;

public interface IProviderRepository
{
    Task<IReadOnlyList<ProviderRecord>> ListAsync(CancellationToken ct = default);
}
