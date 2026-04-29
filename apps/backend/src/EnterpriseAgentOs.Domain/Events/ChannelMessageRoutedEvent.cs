using EnterpriseAgentOs.Domain.Features.Analytics;

namespace EnterpriseAgentOs.Domain.Events;

public sealed record ChannelMessageRoutedEvent(Guid? AgentId, AgentLogType LogType, string ChannelType, string Content, string CorrelationId) : DomainEvent;
