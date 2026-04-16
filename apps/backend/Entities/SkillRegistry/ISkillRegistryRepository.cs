namespace EnterpriseAgentOs.Api.Entities.SkillRegistry;

public interface ISkillRegistryRepository
{
    Task<List<EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord> CreateAsync(EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord record, CancellationToken ct = default);
    Task UpdateAsync(EnterpriseAgentOs.Api.Database.Models.SkillRegistryRecord record, CancellationToken ct = default);
    Task DeleteByNameAsync(string name, CancellationToken ct = default);
}
