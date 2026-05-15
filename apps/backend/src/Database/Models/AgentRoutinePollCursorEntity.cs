namespace OffceOs.Database.Models;

public sealed class AgentRoutinePollCursorEntity
{
    public Guid Id { get; set; }
    public Guid TriggerId { get; set; }
    public string Event { get; set; } = string.Empty;
    public DateTime CursorAt { get; set; }
    public DateTime? LastPolledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AgentRoutineTriggerEntity? Trigger { get; set; }
}
