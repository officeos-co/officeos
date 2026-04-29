namespace EnterpriseAgentOs.Domain.Features.Agents;

/// <summary>
/// Global-per-installation install state + encrypted credentials for a
/// first-party skill. One row per skill name. Credentials are a JSON
/// blob wrapped with the DataProtection pipeline — see
/// <see cref="SkillCredentialProtector"/>.
/// </summary>
public sealed class SkillCredentialRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public string SkillName { get; init; } = string.Empty;

    public bool Enabled { get; set; }

    /// <summary>Protected JSON object of credential key → value.</summary>
    public string? EncryptedCredentials { get; set; }

    public DateTime? ConfiguredAt { get; set; }

    public RunTarget? RunTarget { get; set; }
}
