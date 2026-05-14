namespace OffceOs.Domain.Events;

public sealed record PodConnectedEvent(Guid AgentId, string CorrelationId, int DurationMs) : DomainEvent;
