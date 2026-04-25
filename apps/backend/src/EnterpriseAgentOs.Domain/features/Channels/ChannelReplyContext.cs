using System.Collections.Concurrent;

namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// In-memory store that maps a turn's correlationId to the channel it should
/// reply to. Set on inbound, read on outbound, evicted after use or TTL.
/// Singleton — no database involved.
/// </summary>
public sealed class ChannelReplyContext
{
    private readonly ConcurrentDictionary<string, Entry> _pending = new();

    public void Set(string correlationId, string channelType, string platformId, string? threadId)
    {
        _pending[correlationId] = new Entry(channelType, platformId, threadId, DateTime.UtcNow);
    }

    public (string ChannelType, string PlatformId, string? ThreadId)? Take(string correlationId)
    {
        if (!_pending.TryRemove(correlationId, out var entry))
            return null;
        return (entry.ChannelType, entry.PlatformId, entry.ThreadId);
    }

    /// <summary>Remove entries older than the TTL. Called periodically.</summary>
    public void Evict(TimeSpan ttl)
    {
        var cutoff = DateTime.UtcNow - ttl;
        foreach (var kvp in _pending)
        {
            if (kvp.Value.CreatedAt < cutoff)
                _pending.TryRemove(kvp.Key, out _);
        }
    }

    private sealed record Entry(string ChannelType, string PlatformId, string? ThreadId, DateTime CreatedAt);
}
