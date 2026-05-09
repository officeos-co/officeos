namespace EnterpriseAgentOs.Domain.Features.Integrations;

public sealed class AgentIntegrationRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string IntegrationName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
