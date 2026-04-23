using MediatR;

namespace EnterpriseAgentOs.Application.EventHandlers;

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
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                using var scope = _scopeFactory.CreateScope();
                var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();
                _logger.LogInformation("Attempting test message for connection {Id}", notification.ConnectionId);
                await channelService.SendTestMessageAsync(notification.ConnectionId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background test message failed for {Id}", notification.ConnectionId);
            }
        });

        return Task.CompletedTask;
    }
}
