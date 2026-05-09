using MediatR;

namespace EnterpriseAgentOs.Application.Features.Channels;

internal sealed class BroadcastToChannelsHandler : INotificationHandler<MessageOutEvent>
{
    private readonly ChannelReplyContext _replyContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastToChannelsHandler> _logger;

    public BroadcastToChannelsHandler(
        ChannelReplyContext replyContext,
        IServiceScopeFactory scopeFactory,
        ILogger<BroadcastToChannelsHandler> logger)
    {
        _replyContext = replyContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(MessageOutEvent notification, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(notification.Content))
            return Task.CompletedTask;

        // Check if this turn was triggered by a channel message
        var reply = _replyContext.Take(notification.CorrelationId);
        if (reply is null)
        {
            _logger.LogDebug("No reply context for correlation {CorrelationId} — message not from a channel",
                notification.CorrelationId);
            return Task.CompletedTask;
        }

        var (channelType, platformId, threadId, channelConnectionId) = reply.Value;
        var agentId = notification.AgentId;
        var correlationId = notification.CorrelationId;
        var content = notification.Content;

        BackgroundWork.Run<IChannelGateway, IPublisher>(
            _scopeFactory,
            async (gateway, publisher) =>
            {
                try
                {
                    await gateway.SendAsync(channelType, platformId, threadId,
                        ChannelMessage.Text(content), CancellationToken.None);

                    await publisher.Publish(new ChannelMessageRoutedEvent(
                        agentId, AgentLogType.ChannelOut, channelType, content, correlationId, channelConnectionId));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Channel reply failed for {ChannelType} platformId={PlatformId}",
                        channelType, platformId);

                    await publisher.Publish(new ChannelMessageRoutedEvent(
                        agentId, AgentLogType.Error, channelType,
                        $"Failed to deliver reply via {channelType}: {ex.Message}", correlationId, channelConnectionId));
                }
            },
            _logger);

        return Task.CompletedTask;
    }
}
