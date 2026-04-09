using EnterpriseAgentOs.Api.Entities.Providers.Interfaces;
using EnterpriseAgentOs.Api.Entities.Providers.Models;

namespace EnterpriseAgentOs.Api.Entities.Providers.Services;

public sealed class ProviderService : IProviderService
{
    private readonly IProviderRepository _repository;

    public ProviderService(IProviderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        return records
            .Select(p => new ProviderDto(p.Id, p.Name, p.DisplayName, p.Configured))
            .ToList();
    }
}
