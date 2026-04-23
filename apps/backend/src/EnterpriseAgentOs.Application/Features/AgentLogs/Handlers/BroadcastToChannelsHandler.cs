using MediatR;

namespace EnterpriseAgentOs.Application.Features.AgentLogs.Handlers;

internal sealed class BroadcastToChannelsHandler : INotificationHandler<MessageOutEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastToChannelsHandler> _logger;

    public BroadcastToChannelsHandler(IServiceScopeFactory scopeFactory, ILogger<BroadcastToChannelsHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(MessageOutEvent notification, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(notification.Content))
            return Task.CompletedTask;

        BackgroundWork.Run<IChannelService>(
            _scopeFactory,
            svc => svc.BroadcastAsync(notification.AgentId, notification.Content, CancellationToken.None),
            _logger);

        return Task.CompletedTask;
    }
}
