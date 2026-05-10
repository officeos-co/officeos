using OffceOs.Domain.Features.Channels;

namespace OffceOs.Tests.Channels;

internal sealed class RecordingChannelGateway : IChannelGateway
{
    public int ReloadCount { get; private set; }
    public List<SendCall> SendCalls { get; } = [];

    public Task SendAsync(
        Guid connectionId,
        string channelType,
        string platformId,
        string? threadId,
        ChannelMessage message,
        CancellationToken ct = default)
    {
        SendCalls.Add(new SendCall(connectionId, channelType, platformId, threadId, message));
        return Task.CompletedTask;
    }

    public Task ReloadAsync(CancellationToken ct = default)
    {
        ReloadCount++;
        return Task.CompletedTask;
    }
}

internal sealed record SendCall(
    Guid ConnectionId,
    string ChannelType,
    string PlatformId,
    string? ThreadId,
    ChannelMessage Message);
