using EnterpriseAgentOs.Domain.Features.Agents;

namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentToolPermissionEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public ToolPermission Permission { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
}
