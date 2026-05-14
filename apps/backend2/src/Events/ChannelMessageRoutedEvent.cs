namespace OffceOs.Domain.Events;

public sealed record ChannelMessageRoutedEvent(
    Guid? AgentId,
    AgentLogType LogType,
    string ChannelType,
    string Content,
    string CorrelationId,
    Guid? ChannelConnectionId = null) : DomainEvent;
