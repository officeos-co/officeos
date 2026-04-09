using EnterpriseAgentOs.Api.Entities.Skills.Models;

namespace EnterpriseAgentOs.Api.Entities.Skills.Interfaces;

public interface ISkillRepository
{
    Task<IReadOnlyList<SkillRecord>> ListAsync(CancellationToken ct = default);
}
