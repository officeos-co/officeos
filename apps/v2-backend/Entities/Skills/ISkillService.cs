namespace EnterpriseAgentOs.Api.Entities.Skills;

public interface ISkillService
{
    Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default);
    Task<SkillDto?> GetAsync(string name, CancellationToken ct = default);
    Task<SkillDto?> InstallAsync(string name, CancellationToken ct = default);
    Task<SkillDto?> UninstallAsync(string name, CancellationToken ct = default);
    Task<SkillDto?> PutCredentialsAsync(
        string name,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the decrypted credentials for a skill if it's installed and
    /// configured, or <c>null</c> otherwise. Used by the execution routes.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>?> GetDecryptedCredentialsAsync(
        string name,
        CancellationToken ct = default);

    Task<CapabilitiesResponse> ListCapabilitiesAsync(CancellationToken ct = default);
}
