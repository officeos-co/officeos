namespace EnterpriseAgentOs.Domain.Interfaces.Skills;

public interface ISkillCatalogRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillRecord>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillRecord>> ListActiveAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillRecord> UpsertAsync(EnterpriseAgentOs.Domain.Models.SkillRecord record, CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default);
}
