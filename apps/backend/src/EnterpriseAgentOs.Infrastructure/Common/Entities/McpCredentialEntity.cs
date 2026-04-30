namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class McpCredentialEntity
{
    public Guid Id { get; set; }
    public string McpServerName { get; set; } = string.Empty;
    public string EncryptedCredentials { get; set; } = string.Empty;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}
