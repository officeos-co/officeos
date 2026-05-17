namespace OffceOs.Features.Agents.Domain;

public sealed class AgentSessionContextRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public Guid SessionId { get; init; }
    public string Summary { get; set; } = string.Empty;
    public Guid? LastCompactedLogId { get; set; }
    public DateTime? LastCompactedAt { get; set; }
    public int PreCompactTokens { get; set; }
    public int PostCompactTokens { get; set; }
    public int CompactionVersion { get; set; } = 1;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
