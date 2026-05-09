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
}
