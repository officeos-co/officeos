using EnterpriseAgentOs.Api.Entities.Skills.Models;

namespace EnterpriseAgentOs.Api.Entities.Skills.Interfaces;

public interface ISkillService
{
    Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default);
}
