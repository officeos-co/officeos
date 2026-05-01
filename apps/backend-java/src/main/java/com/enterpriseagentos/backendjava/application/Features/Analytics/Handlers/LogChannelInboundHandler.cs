using MediatR;

namespace EnterpriseAgentOs.Application.Features.Analytics;

internal sealed class LogChannelInboundHandler : INotificationHandler<ChannelMessageRoutedEvent>
{
    private readonly IAgentLogRepository _agentLogRepository;

    public LogChannelInboundHandler(IAgentLogRepository agentLogRepository)
        => _agentLogRepository = agentLogRepository;

    public async Task Handle(ChannelMessageRoutedEvent notification, CancellationToken ct)
    {
        await _agentLogRepository.AppendAsync(new AgentLogRecord
        {
            AgentId = notification.AgentId,
            Type = notification.LogType,
            Channel = notification.ChannelType,
            Content = notification.Content,
            CorrelationId = notification.CorrelationId,
        }, ct);
    }
}
