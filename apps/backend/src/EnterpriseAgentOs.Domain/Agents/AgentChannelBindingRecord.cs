namespace EnterpriseAgentOs.Domain.Agents;

/// <summary>
/// Per-agent channel binding. Links an agent to an org-level channel connection
/// with agent-specific config (DM policy, allowed users, mention patterns, etc).
/// </summary>
public sealed class AgentChannelBindingRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>FK to AgentRecord.</summary>
    public Guid AgentId { get; set; }
    public AgentRecord? Agent { get; set; }

    /// <summary>FK to ChannelConnectionRecord.</summary>
    public Guid ChannelConnectionId { get; set; }
    public ChannelConnectionRecord? ChannelConnection { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// JSON object with agent-specific channel config:
    /// dmPolicy, groupPolicy, allowedUsers[], allowedGroups[],
    /// requireMention, mentionPatterns[], historyLimit, streamingMode.
    /// Stored as plaintext — no secrets here.
    /// </summary>
    public string? Config { get; set; }

    /// <summary>
    /// Platform-specific destination for outbound messages (Slack channel ID,
    /// Discord channel ID, WhatsApp JID, etc.). Updated on every inbound message.
    /// </summary>
    public string? LastChannelId { get; set; }

    /// <summary>
    /// Last sender identifier (used for WhatsApp JID, Telegram chat ID, etc.).
    /// Updated on every inbound message.
    /// </summary>
    public string? LastSenderIdentifier { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // ── Domain logic ─────────────────────────────────────────────────

    /// <summary>Whether this binding has a known reply-to destination.</summary>
    public bool HasReplyContext => !string.IsNullOrEmpty(LastChannelId) || !string.IsNullOrEmpty(LastSenderIdentifier);

    /// <summary>Best destination for outbound messages.</summary>
    public string? ReplyDestination => LastChannelId ?? LastSenderIdentifier;

    /// <summary>Update the reply-to context from an inbound message.</summary>
    public void UpdateReplyContext(string senderIdentifier, string? channelId)
    {
        LastSenderIdentifier = senderIdentifier;
        LastChannelId = channelId ?? senderIdentifier;
    }
}
