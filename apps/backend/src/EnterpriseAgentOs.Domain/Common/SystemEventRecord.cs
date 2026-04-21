namespace EnterpriseAgentOs.Domain.Common;

public sealed class SystemEventRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>error | warning | info</summary>
    [Required, MaxLength(16)]
    public string Severity { get; set; } = "error";

    /// <summary>skill_execution | skill_build | agent | provider | system</summary>
    [Required, MaxLength(64)]
    public string Category { get; set; } = "system";

    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>Structured context (stack trace, request payload, etc.)</summary>
    public string? DetailJson { get; set; }

    [MaxLength(64)]
    public string? SkillName { get; set; }

    public Guid? AgentId { get; set; }

    [MaxLength(128)]
    public string? CorrelationId { get; set; }

    public bool Acknowledged { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
