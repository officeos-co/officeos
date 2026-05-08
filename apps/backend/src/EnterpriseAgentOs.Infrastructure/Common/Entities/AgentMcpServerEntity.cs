namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentIntegrationDefinitionEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
