namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public sealed class IntegrationCredentialRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string IntegrationName { get; init; } = string.Empty;
    public string EncryptedCredentials { get; init; } = string.Empty;
    public DateTime ConfiguredAt { get; init; } = DateTime.UtcNow;
}
