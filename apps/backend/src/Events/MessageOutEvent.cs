namespace OffceOs.Domain.Events;

public sealed record MessageOutEvent(Guid AgentId, string CorrelationId, string Content) : DomainEvent;
