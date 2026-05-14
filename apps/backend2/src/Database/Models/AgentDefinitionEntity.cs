namespace OffceOs.Database.Models;

public sealed class AgentDefinitionEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string ConfigHash { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
}
