using MediatR;

namespace EnterpriseAgentOs.Application.Features.AgentLogs.Handlers;

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
            return Task.CompletedTask;

        var (channelType, platformId, threadId) = reply.Value;

        BackgroundWork.Run<IChannelGateway>(
            _scopeFactory,
            gateway => gateway.SendAsync(channelType, platformId, threadId,
                ChannelMessage.Text(notification.Content), CancellationToken.None),
            _logger);

        return Task.CompletedTask;
    }
}
