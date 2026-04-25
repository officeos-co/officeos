using EnterpriseAgentOs.Domain.Features.AgentLogs;

namespace EnterpriseAgentOs.Domain.Events;

public sealed record ChannelMessageRoutedEvent(Guid? AgentId, AgentLogType LogType, string ChannelType, string Content, string CorrelationId) : DomainEvent;
