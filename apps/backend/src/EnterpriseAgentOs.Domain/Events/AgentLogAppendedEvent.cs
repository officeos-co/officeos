using EnterpriseAgentOs.Domain.Features.AgentLogs;

namespace EnterpriseAgentOs.Domain.Events;

public sealed record AgentLogAppendedEvent(AgentLogRecord Record) : DomainEvent;
