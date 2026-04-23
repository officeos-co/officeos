using MediatR;

namespace EnterpriseAgentOs.Application.EventHandlers;

internal sealed class BroadcastToGraphQLHandler : INotificationHandler<AgentLogAppendedEvent>
{
    private readonly ITopicEventSender _topicEventSender;

    public BroadcastToGraphQLHandler(ITopicEventSender topicEventSender)
        => _topicEventSender = topicEventSender;

    public async Task Handle(AgentLogAppendedEvent notification, CancellationToken ct)
    {
        var record = notification.Record;
        await _topicEventSender.SendAsync($"agent-log:{record.AgentId}", record.ToDto(), ct);
    }
}
