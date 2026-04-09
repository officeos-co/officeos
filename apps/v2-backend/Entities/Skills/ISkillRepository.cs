
namespace EnterpriseAgentOs.Api.Entities.Skills;

public interface ISkillRepository
{
    Task<IReadOnlyList<SkillRecord>> ListAsync(CancellationToken ct = default);
}
