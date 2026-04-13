using System.Threading.Channels;
using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.Events;

/// <summary>
/// In-memory fan-out broadcaster for system events.
/// Singleton — each SSE client subscribes and receives a dedicated channel.
/// </summary>
public sealed class SystemEventBroadcaster
{
    private readonly Lock _lock = new();
    private readonly List<Channel<SystemEventRecord>> _subscribers = [];

    public void Publish(SystemEventRecord ev)
    {
        lock (_lock)
        {
            // Remove dead subscribers while publishing
            for (int i = _subscribers.Count - 1; i >= 0; i--)
            {
                if (!_subscribers[i].Writer.TryWrite(ev))
                {
                    _subscribers[i].Writer.TryComplete();
                    _subscribers.RemoveAt(i);
                }
            }
        }
    }

    public ChannelReader<SystemEventRecord> Subscribe()
    {
        var channel = Channel.CreateBounded<SystemEventRecord>(
            new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

        lock (_lock)
        {
            _subscribers.Add(channel);
        }

        return channel.Reader;
    }

    public void Unsubscribe(ChannelReader<SystemEventRecord> reader)
    {
        lock (_lock)
        {
            for (int i = _subscribers.Count - 1; i >= 0; i--)
            {
                if (_subscribers[i].Reader == reader)
                {
                    _subscribers[i].Writer.TryComplete();
                    _subscribers.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
