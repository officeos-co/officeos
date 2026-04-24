namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class SkillCredentialEntity
{
    public Guid Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? EncryptedCredentials { get; set; }
    public DateTime? ConfiguredAt { get; set; }
    public string? RunTarget { get; set; }
}
