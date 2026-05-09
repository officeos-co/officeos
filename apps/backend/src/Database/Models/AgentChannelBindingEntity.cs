namespace EnterpriseAgentOs.Database.Models;

public sealed class AgentChannelBindingEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid ChannelConnectionId { get; set; }
    public bool Enabled { get; set; }
    public string? Config { get; set; }
    public DateTime CreatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
    public ChannelConnectionEntity? ChannelConnection { get; set; }
}
