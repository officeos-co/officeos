namespace OffceOs.Domain.Features.Agents;

public sealed class AgentRunRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public Guid? ParentRunId { get; init; }
    public string? ParentCorrelationId { get; init; }
    public string Kind { get; init; } = "turn";
    public string Status { get; set; } = "running";
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string? Result { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
