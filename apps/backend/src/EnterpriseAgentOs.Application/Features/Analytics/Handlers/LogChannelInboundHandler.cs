using MediatR;

namespace EnterpriseAgentOs.Application.Features.Analytics;

internal sealed class LogChannelInboundHandler : INotificationHandler<ChannelMessageRoutedEvent>
{
    private readonly IAgentLogService _agentLogService;

    public LogChannelInboundHandler(IAgentLogService agentLogService)
        => _agentLogService = agentLogService;

    public async Task Handle(ChannelMessageRoutedEvent notification, CancellationToken ct)
    {
        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = notification.AgentId,
            Type = notification.LogType,
            Channel = notification.ChannelType,
            ChannelConnectionId = notification.ChannelConnectionId,
            Content = notification.Content,
            CorrelationId = notification.CorrelationId,
        }, ct);
    }
}
