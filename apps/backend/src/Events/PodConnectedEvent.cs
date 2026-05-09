namespace EnterpriseAgentOs.Domain.Events;

public sealed record PodConnectedEvent(Guid AgentId, string CorrelationId, int DurationMs) : DomainEvent;
