namespace OffceOs.Domain.Features.Channels;

public sealed record ChannelMessageRoutedEvent(
    Guid? AgentId,
    AgentLogType LogType,
    string ChannelType,
    string Content,
    string CorrelationId,
    Guid? ChannelConnectionId = null) : DomainEvent;
