namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class AgentMemoryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; set; }

    [Required, MaxLength(512)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string Category { get; set; } = "core";

    [MaxLength(128)]
    public string Namespace { get; set; } = "default";

    [MaxLength(256)]
    public string? SessionId { get; set; }

    public double? Importance { get; set; }

    [MaxLength(512)]
    public string? SupersededBy { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
