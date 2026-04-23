using EnterpriseAgentOs.Domain.Agents;

namespace EnterpriseAgentOs.Domain.Events;

public sealed record AgentLogAppendedEvent(AgentLogRecord Record) : DomainEvent;
