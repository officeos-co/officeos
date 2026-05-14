namespace OffceOs.Domain.Features.Channels;

/// <summary>
/// Typed shape for <see cref="AgentChannelBindingRecord.Config"/>. Stored as JSON in that
/// column so existing rows do not migrate. Used by GraphQL inputs / outputs so the dashboard
/// and backend agree on the shape without duplication.
/// </summary>
public sealed class ChannelBindingConfig
{
    /// <summary>Platform-specific delivery target (channel ID, chat ID, etc.)</summary>
    public string? PlatformId { get; set; }

    /// <summary>Platform-specific thread/topic ID for threaded delivery.</summary>
    public string? ThreadId { get; set; }

    public bool? RequireMention { get; set; }

    public string? BotUserId { get; set; }

    public string? BotMention { get; set; }

    public string? ReplyToMode { get; set; }

    public int? InitialHistoryLimit { get; set; }

    public int? HistoryLimit { get; set; }

    public IReadOnlyList<string>? AllowedSenderIds { get; set; }

    public IReadOnlyList<string>? AllowedGroupIds { get; set; }

    public IReadOnlyList<string>? MentionPatterns { get; set; }

    public IReadOnlyList<string>? ActiveTopicIds { get; set; }

    public bool? CanSend { get; set; }

    public bool? CanReceive { get; set; }

    public bool? ReplyOnly { get; set; }

    public string? Label { get; set; }
}
