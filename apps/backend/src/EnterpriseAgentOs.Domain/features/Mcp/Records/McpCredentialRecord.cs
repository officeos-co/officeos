namespace EnterpriseAgentOs.Domain.Features.Mcp;

public sealed class McpCredentialRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string McpServerName { get; init; } = string.Empty;
    public string EncryptedCredentials { get; init; } = string.Empty;
    public DateTime ConfiguredAt { get; init; } = DateTime.UtcNow;
}
