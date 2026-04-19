namespace EnterpriseAgentOs.Domain.Interfaces.Skills;

public interface ISkillRepository
{
    Task<IReadOnlyList<SkillCredentialRecord>> ListAsync(CancellationToken ct = default);
    Task<SkillCredentialRecord?> GetByNameAsync(string skillName, CancellationToken ct = default);
    Task<SkillCredentialRecord> UpsertAsync(
        string skillName,
        bool? enabled,
        string? encryptedCredentials,
        CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string skillName, CancellationToken ct = default);
    Task SetRunTargetAsync(string skillName, string? runTarget, CancellationToken ct = default);
}
