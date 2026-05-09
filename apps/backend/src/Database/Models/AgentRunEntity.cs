namespace OffceOs.Database.Models;

public sealed class AgentRunEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid? ParentRunId { get; set; }
    public string? ParentCorrelationId { get; set; }
    public string Kind { get; set; } = "turn";
    public string Status { get; set; } = "running";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AgentEntity? Agent { get; set; }
}
