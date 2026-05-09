namespace OffceOs.Domain.Common;

public sealed class SystemEventRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>error | warning | info</summary>
    [Required, MaxLength(16)]
    public string Severity { get; init; } = "error";

    /// <summary>skill_execution | skill_build | agent | provider | system</summary>
    [Required, MaxLength(64)]
    public string Category { get; init; } = "system";

    [Required]
    public string Message { get; init; } = string.Empty;

    /// <summary>Structured context (stack trace, request payload, etc.)</summary>
    public string? DetailJson { get; init; }

    [MaxLength(64)]
    public string? SkillName { get; init; }

    public Guid? AgentId { get; init; }

    [MaxLength(128)]
    public string? CorrelationId { get; init; }

    public bool Acknowledged { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
