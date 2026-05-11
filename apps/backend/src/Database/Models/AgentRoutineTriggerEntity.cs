namespace OffceOs.Database.Models;

public sealed class AgentRoutineTriggerEntity
{
    public Guid Id { get; set; }
    public Guid RoutineId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string? SecretHash { get; set; }
    public string? EncryptedSecret { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public AgentRoutineEntity? Routine { get; set; }
}
