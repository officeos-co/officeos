namespace OffceOs.Database.Models;

public sealed class AgentRoutineEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
    public List<AgentRoutineTriggerEntity> Triggers { get; set; } = [];
}
