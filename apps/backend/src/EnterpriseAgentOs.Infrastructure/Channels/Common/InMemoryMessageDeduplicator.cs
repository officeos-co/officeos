using System.Collections.Concurrent;

namespace EnterpriseAgentOs.Infrastructure.Channels.Common;

/// <summary>
/// In-memory message deduplication with a 5-minute sliding window.
/// Prevents duplicate processing of webhook retries and echo messages.
/// </summary>
public sealed class InMemoryMessageDeduplicator : IDisposable
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new();
    private readonly Timer _cleanupTimer;
    private static readonly TimeSpan ExpirationWindow = TimeSpan.FromMinutes(5);

    public InMemoryMessageDeduplicator()
    {
        _cleanupTimer = new Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Returns true if this message has already been seen (duplicate).
    /// If not seen, tracks it and returns false.
    /// </summary>
    public bool IsDuplicate(string platform, string messageId)
    {
        if (string.IsNullOrEmpty(messageId)) return false;

        var key = $"{platform}:{messageId}";
        var now = DateTimeOffset.UtcNow;

        if (_seen.TryGetValue(key, out var existing) && now - existing < ExpirationWindow)
            return true;

        _seen[key] = now;
        return false;
    }

    /// <summary>
    /// Track an outbound message ID to filter echo messages.
    /// </summary>
    public void TrackOutbound(string platform, string messageId)
    {
        if (string.IsNullOrEmpty(messageId)) return;
        _seen[$"{platform}:out:{messageId}"] = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Check if a message ID was sent by us (outbound echo).
    /// </summary>
    public bool IsOwnMessage(string platform, string messageId)
    {
        if (string.IsNullOrEmpty(messageId)) return false;
        return _seen.ContainsKey($"{platform}:out:{messageId}");
    }

    private void Cleanup()
    {
        var cutoff = DateTimeOffset.UtcNow - ExpirationWindow;
        foreach (var kvp in _seen)
        {
            if (kvp.Value < cutoff)
                _seen.TryRemove(kvp.Key, out _);
        }
    }

    public void Dispose() => _cleanupTimer.Dispose();
}
