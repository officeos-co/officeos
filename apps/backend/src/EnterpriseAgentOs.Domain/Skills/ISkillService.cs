namespace EnterpriseAgentOs.Domain.Skills;

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

    Task<CapabilitiesResponse> ListCapabilitiesAsync(Guid? agentId = null, CancellationToken ct = default);

    //todo we dont eevn have run target
    /// <summary>
    /// Sets where a skill executes: "cloud" (default) or "runner" (self-hosted).
    /// </summary>
    Task<SkillDto?> SetRunTargetAsync(string name, string runTarget, CancellationToken ct = default);

    /// <summary>
    /// Returns the run target for a skill ("cloud" or "runner").
    /// </summary>
    Task<string> GetRunTargetAsync(string name, CancellationToken ct = default);
}
