namespace EnterpriseAgentOs.Domain.Features.AgentSkills;

/// <summary>
/// Allow/deny decision for a single (skill, tool) pair on a given agent.
/// Set from the Quickstart wizard and the per-agent skills tab.
/// Absence of a row means the tool is disabled — only explicit <see cref="Allow"/> grants access.
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

    public Guid AgentId { get; init; }
    public AgentRecord? Agent { get; init; }

    [Required, MaxLength(64)]
    public string SkillName { get; init; }

    [Required, MaxLength(64)]
    public string ToolName { get; init; }

    public ToolPermission Permission { get; set; } = ToolPermission.Allow;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Parses a "skill:tool" key into its components. Keys without ":"
    /// are treated as skill-level defaults with an empty tool name.
    /// </summary>
    public static ToolKey ParseToolKey(string key) => ToolKey.Parse(key);
}
