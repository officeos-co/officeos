using EnterpriseAgentOs.Api.Entities.Skills.Interfaces;
using EnterpriseAgentOs.Api.Entities.Skills.Models;

namespace EnterpriseAgentOs.Api.Entities.Skills.Services;

public sealed class SkillService : ISkillService
{
    private readonly ISkillRepository _repository;

    public SkillService(ISkillRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        return records
            .Select(s => new SkillDto(s.Id, s.Name, s.DisplayName, s.Installed))
            .ToList();
    }
}
