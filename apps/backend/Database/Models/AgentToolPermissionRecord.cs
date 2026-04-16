namespace EnterpriseAgentOs.Api.Database.Models;

/// <summary>
/// Allow/deny decision for a single (skill, tool) pair on a given agent.
/// Set from the Quickstart wizard and the per-agent skills tab.
/// Absence of a row means the tool is disabled — only explicit <see cref="ToolPermission.Allow"/> grants access.
/// Enforced in the skill execution path before dispatch.
/// </summary>
public enum ToolPermission
{
    Allow,
    Deny,
}

public sealed class AgentToolPermissionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid AgentId { get; set; }
    public AgentRecord? Agent { get; set; }

    [Required, MaxLength(64)]
    public string SkillName { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string ToolName { get; set; } = string.Empty;

    public ToolPermission Permission { get; set; } = ToolPermission.Allow;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
