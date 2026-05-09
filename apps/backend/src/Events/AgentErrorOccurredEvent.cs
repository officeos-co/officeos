namespace OffceOs.Domain.Events;

public sealed record AgentErrorOccurredEvent(Guid AgentId, string CorrelationId, string Message) : DomainEvent;
