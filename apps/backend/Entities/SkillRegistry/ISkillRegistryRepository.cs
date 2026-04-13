namespace EnterpriseAgentOs.Api.Entities.SkillRegistry;

public interface ISkillRegistryRepository
{
    Task<List<SkillRegistryRecord>> ListAsync(CancellationToken ct = default);
    Task<SkillRegistryRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<SkillRegistryRecord> CreateAsync(SkillRegistryRecord record, CancellationToken ct = default);
    Task UpdateAsync(SkillRegistryRecord record, CancellationToken ct = default);
    Task DeleteByNameAsync(string name, CancellationToken ct = default);
}
