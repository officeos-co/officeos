namespace OffceOs.Domain.Features.AgentHarness;

public sealed record AgentErrorOccurredEvent(Guid AgentId, string CorrelationId, string Message) : DomainEvent;
