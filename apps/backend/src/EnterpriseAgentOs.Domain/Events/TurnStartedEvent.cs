namespace EnterpriseAgentOs.Domain.Events;

public sealed record TurnStartedEvent(Guid AgentId, string CorrelationId, string UserMessage) : DomainEvent;
