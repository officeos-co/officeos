using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class SendTestMessageHandler : INotificationHandler<ChannelCredsStoredEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SendTestMessageHandler> _logger;

    public SendTestMessageHandler(IServiceScopeFactory scopeFactory, ILogger<SendTestMessageHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(ChannelCredsStoredEvent notification, CancellationToken ct)
    {
        BackgroundWork.Run<IChannelService>(
            _scopeFactory,
            svc => svc.SendTestMessageAsync(notification.ConnectionId, CancellationToken.None),
            _logger,
            delay: TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }
}
