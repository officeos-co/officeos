namespace EnterpriseAgentOs.Api.Database.Models;

/// <summary>
/// Global-per-installation install state + encrypted credentials for a
/// first-party skill. One row per skill name. Credentials are a JSON
/// blob wrapped with the DataProtection pipeline — see
/// <see cref="EnterpriseAgentOs.Api.Entities.Skills.SkillCredentialProtector"/>.
/// </summary>
public sealed class SkillCredentialRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public string SkillName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    /// <summary>Protected JSON object of credential key → value.</summary>
    public string? EncryptedCredentials { get; set; }

    public DateTime? ConfiguredAt { get; set; }
}
