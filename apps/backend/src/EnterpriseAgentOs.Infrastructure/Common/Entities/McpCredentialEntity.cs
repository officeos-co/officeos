namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class IntegrationCredentialEntity
{
    public Guid Id { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string EncryptedCredentials { get; set; } = string.Empty;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}
