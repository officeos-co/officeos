namespace EnterpriseAgentOs.Domain.Models;

/// <summary>
/// Per-interaction permission for a channel.
/// <list type="bullet">
///   <item><c>Allow</c> — silently permit.</item>
///   <item><c>Ask</c> — require human approval via the log.</item>
///   <item><c>Deny</c> — reject outright.</item>
/// </list>
/// </summary>
public enum ChannelPermission
{
    Allow,
    Ask,
    Deny,
}

/// <summary>
/// Typed shape for <see cref="AgentChannelBindingRecord.Config"/>. Stored as JSON in that
/// column so existing rows do not migrate. Used by GraphQL inputs / outputs so the dashboard
/// and backend agree on the shape without duplication.
/// </summary>
public sealed class ChannelBindingConfig
{
    public ChannelPermission Receive { get; set; } = ChannelPermission.Allow;
    public ChannelPermission Send { get; set; } = ChannelPermission.Allow;
    public ChannelPermission Initiate { get; set; } = ChannelPermission.Ask;

    // Legacy per-agent channel fields (preserved — older code reads these from the JSON blob).
    public string? DmPolicy { get; set; }
    public string? GroupPolicy { get; set; }
    public List<string>? AllowedUsers { get; set; }
    public List<string>? AllowedGroups { get; set; }
    public bool? RequireMention { get; set; }
    public List<string>? MentionPatterns { get; set; }
    public int? HistoryLimit { get; set; }
    public string? StreamingMode { get; set; }
}
