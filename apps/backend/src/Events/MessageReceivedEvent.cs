namespace OffceOs.Domain.Events;

public sealed record MessageReceivedEvent(Guid AgentId, string Content, string CorrelationId) : DomainEvent;
