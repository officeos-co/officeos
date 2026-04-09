
namespace EnterpriseAgentOs.Api.Entities.Skills;

public interface ISkillService
{
    Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default);
}
