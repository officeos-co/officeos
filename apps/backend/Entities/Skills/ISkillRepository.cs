namespace EnterpriseAgentOs.Api.Entities.Skills;

public interface ISkillRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.SkillCredentialRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.SkillCredentialRecord?> GetByNameAsync(string skillName, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.SkillCredentialRecord> UpsertAsync(
        string skillName,
        bool? enabled,
        string? encryptedCredentials,
        CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string skillName, CancellationToken ct = default);
    Task SetRunTargetAsync(string skillName, string? runTarget, CancellationToken ct = default);
}
