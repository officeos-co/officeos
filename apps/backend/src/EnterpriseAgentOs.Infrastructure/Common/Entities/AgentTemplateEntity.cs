namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentTemplateEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string IntegrationsJson { get; set; } = "[]";
    public string ChannelsJson { get; set; } = "[]";
    public bool IsBuiltin { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserEntity? Owner { get; set; }
}
