namespace OffceOs.Features.Channels.Domain;

/// <summary>
/// In-memory store that maps a turn's correlationId to the channel it should
/// reply to. Set on inbound, read on outbound, evicted after use or TTL.
/// Singleton — no database involved.
/// </summary>
public sealed class ChannelReplyContext
{
    private readonly ConcurrentDictionary<string, Entry> _pending = new();
    private readonly ConcurrentDictionary<string, InternalEntry> _internalPending = new();

    public void Set(string correlationId, string channelType, string platformId, string? threadId, Guid channelConnectionId)
    {
        _pending[correlationId] = new Entry(channelType, platformId, threadId, channelConnectionId);
    }

    public (string ChannelType, string PlatformId, string? ThreadId, Guid ChannelConnectionId)? Take(string correlationId)
    {
        if (!_pending.TryRemove(correlationId, out var entry))
            return null;
        return (entry.ChannelType, entry.PlatformId, entry.ThreadId, entry.ChannelConnectionId);
    }

    public void SetInternal(string correlationId, Guid channelConnectionId, Guid sourceAgentId, Guid replyingAgentId)
    {
        _internalPending[correlationId] = new InternalEntry(channelConnectionId, sourceAgentId, replyingAgentId);
    }

    public (Guid ChannelConnectionId, Guid SourceAgentId, Guid ReplyingAgentId)? TakeInternal(string correlationId)
    {
        if (!_internalPending.TryRemove(correlationId, out var entry))
            return null;
        return (entry.ChannelConnectionId, entry.SourceAgentId, entry.ReplyingAgentId);
    }

    private sealed record Entry(string ChannelType, string PlatformId, string? ThreadId, Guid ChannelConnectionId);
    private sealed record InternalEntry(Guid ChannelConnectionId, Guid SourceAgentId, Guid ReplyingAgentId);
}
