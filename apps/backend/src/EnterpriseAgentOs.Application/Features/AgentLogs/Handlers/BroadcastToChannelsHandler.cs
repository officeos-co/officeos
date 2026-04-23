using MediatR;

namespace EnterpriseAgentOs.Application.Features.AgentLogs.Handlers;

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

        BackgroundWork.Run<IChannelService>(
            _scopeFactory,
            svc => svc.BroadcastAsync(record.AgentId, record.Content, CancellationToken.None),
            _logger);

        return Task.CompletedTask;
    }
}
