namespace OffceOs.EventHandlers.Features.Channels;

internal sealed class SendTestMessageHandler : INotificationHandler<ChannelCredsStoredEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SendTestMessageHandler> _logger;

    public SendTestMessageHandler(IServiceScopeFactory scopeFactory, ILogger<SendTestMessageHandler> logger)
    {
        _serviceScopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(ChannelCredsStoredEvent notification, CancellationToken ct)
    {
        BackgroundWork.Run<IChannelService>(
            _serviceScopeFactory,
            svc => svc.SendTestMessageAsync(notification.ConnectionId, CancellationToken.None),
            _logger,
            delay: TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }
}
