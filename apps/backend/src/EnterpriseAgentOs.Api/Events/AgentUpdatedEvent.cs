namespace EnterpriseAgentOs.Domain.Events;

public sealed record AgentUpdatedEvent(Guid AgentId) : DomainEvent;
