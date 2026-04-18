namespace EnterpriseAgentOs.Domain.Interfaces.Skills;

public interface ISkillRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord?> GetByNameAsync(string skillName, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord> UpsertAsync(
        string skillName,
        bool? enabled,
        string? encryptedCredentials,
        CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string skillName, CancellationToken ct = default);
    Task SetRunTargetAsync(string skillName, string? runTarget, CancellationToken ct = default);
}
