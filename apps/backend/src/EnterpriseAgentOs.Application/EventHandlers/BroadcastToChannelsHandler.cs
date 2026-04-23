using MediatR;

namespace EnterpriseAgentOs.Application.EventHandlers;

internal sealed class BroadcastToChannelsHandler : INotificationHandler<AgentLogAppendedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastToChannelsHandler> _logger;

    public BroadcastToChannelsHandler(IServiceScopeFactory scopeFactory, ILogger<BroadcastToChannelsHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(AgentLogAppendedEvent notification, CancellationToken ct)
    {
        var record = notification.Record;
        if (record.Type != AgentLogType.MessageOut || string.IsNullOrEmpty(record.Content))
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();
                await channelService.BroadcastAsync(record.AgentId, record.Content, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Channel broadcast failed for agent {AgentId}", record.AgentId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
