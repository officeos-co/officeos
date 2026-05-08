namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentIntegrationEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
