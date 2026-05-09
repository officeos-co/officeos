namespace EnterpriseAgentOs.Domain.Features.Agents;

/// <summary>
/// Per-agent channel binding. Links an agent to an org-level channel connection
/// with agent-specific config (DM policy, allowed users, mention patterns, etc).
/// </summary>
public sealed class AgentChannelBindingRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>FK to AgentRecord.</summary>
    public Guid AgentId { get; init; }
    public AgentRecord? Agent { get; init; }

    /// <summary>FK to ChannelConnectionRecord.</summary>
    public Guid ChannelConnectionId { get; init; }
    public ChannelConnectionRecord? ChannelConnection { get; init; }

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// JSON object with agent-specific channel config:
    /// dmPolicy, groupPolicy, allowedUsers[], allowedGroups[],
    /// requireMention, mentionPatterns[], historyLimit, streamingMode.
    /// Stored as plaintext — no secrets here.
    /// </summary>
    public string? Config { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
