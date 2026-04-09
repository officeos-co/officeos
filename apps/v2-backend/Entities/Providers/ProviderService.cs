using EnterpriseAgentOs.Api.Entities.Providers;

namespace EnterpriseAgentOs.Api.Entities.Providers;

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
