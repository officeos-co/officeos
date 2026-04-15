namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class ApprovalRequestRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentId { get; set; }

    [Required, MaxLength(64)]
    public string SkillName { get; set; } = "";

    [Required, MaxLength(64)]
    public string Action { get; set; } = "";

    public string ParamsJson { get; set; } = "{}";

    /// <summary>"pending" | "approved" | "rejected"</summary>
    [Required, MaxLength(16)]
    public string Status { get; set; } = "pending";

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? ResultJson { get; set; }
}
