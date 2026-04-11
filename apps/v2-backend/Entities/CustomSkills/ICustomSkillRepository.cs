namespace EnterpriseAgentOs.Api.Entities.CustomSkills;

public interface ICustomSkillRepository
{
    Task<CustomSkillRecord> CreateAsync(CustomSkillRecord record, CancellationToken ct = default);
    Task<List<CustomSkillRecord>> ListAsync(CancellationToken ct = default);
    Task<CustomSkillRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomSkillRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task UpdateAsync(CustomSkillRecord record, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
