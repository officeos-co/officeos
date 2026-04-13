namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class RunnerJobRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid RunnerId { get; set; }
    [Required, MaxLength(32)]
    public string Status { get; set; } = "pending";
    /// <summary>JSON payload: { skill, action, params, credentials }</summary>
    public string Payload { get; set; } = "{}";
    /// <summary>JSON result from runner.</summary>
    public string? Result { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ClaimDeadline { get; set; }

    public RunnerRecord? Runner { get; set; }
}
